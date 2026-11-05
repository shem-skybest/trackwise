import { HttpContextToken, HttpResponse, type HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { of } from "rxjs";
import { tap } from "rxjs/operators";
import { HttpCacheService } from "../../services/http-cache.service";
import { getBasePath, getInvalidationTargets, getTtlForUrl } from "../cache.config";

const MUTATION_METHODS = new Set(["POST", "PUT", "PATCH", "DELETE"]);

/**
 * Set on a GET whose answer depends on writes to OTHER resources in a way a path rule can't express -
 * e.g. an employee search filtered by the day's crew status, which changes on a `/crew-assignments`
 * write. The request goes straight to the backend and its response isn't cached.
 */
export const SKIP_CACHE = new HttpContextToken<boolean>(() => false);

/**
 * The base path is a delimited token rather than a bare substring of the URL, because
 * `HttpCacheService.invalidate` matches with `includes`: `/tax/jurisdictions` and `/tax-rates` are
 * both real endpoints, and `"…/tax-rates".includes("/tax")` is true. Building the key and the
 * invalidation pattern from this one pair keeps them from drifting apart.
 */
const cacheKey = (tenantId: string, basePath: string, urlWithParams: string): string =>
  `${tenantId}:${basePath}:${urlWithParams}`;

const invalidationPattern = (tenantId: string, basePath: string): string =>
  `${tenantId}:${basePath}:`;

/**
 * HTTP interceptor for caching GET requests based on predefined TTL rules.
 * Invalidates cache on mutation requests (POST, PUT, PATCH, DELETE).
 * @param req The request object
 * @param next The next interceptor in the chain
 * @returns The HTTP event stream
 */
export const cacheInterceptor: HttpInterceptorFn = (req, next) => {
  const cacheService = inject(HttpCacheService);
  const tenantId = req.headers.get("X-Tenant") ?? "";

  // Only cache GET requests
  if (req.method !== "GET") {
    if (!MUTATION_METHODS.has(req.method)) {
      return next(req);
    }

    const patterns = getInvalidationTargets(req.url).map((basePath) =>
      invalidationPattern(tenantId, basePath),
    );
    const evict = (): void => {
      for (const pattern of patterns) {
        cacheService.invalidate(pattern);
      }
    };

    evict();

    // A GET already in flight when the write was sent can land afterwards and repopulate the
    // cache with pre-write data, pinning it for the full TTL. Evict again on acknowledgement.
    return next(req).pipe(
      tap((event) => {
        if (event instanceof HttpResponse) {
          evict();
        }
      }),
    );
  }

  // Get TTL for this URL - check first to skip caching entirely for TTL=0
  const ttl = getTtlForUrl(req.url);
  if (ttl == null || ttl === 0 || req.context.get(SKIP_CACHE)) {
    return next(req); // Don't cache if no rule matches or TTL is 0
  }

  const basePath = getBasePath(req.url);
  const key = cacheKey(tenantId, basePath, req.urlWithParams);

  // Check cache
  const cached = cacheService.get<HttpResponse<unknown>>(key);
  if (cached) {
    return of(cached.clone());
  }

  // Snapshot taken BEFORE the request goes out: if a write to this resource lands while the read
  // is in flight, this response is already stale on arrival and must not be cached, or it pins
  // pre-write data for the entire TTL. Evicting at write time cannot cover this - the write has
  // nothing to evict yet.
  const generationAtRequest = cacheService.generation(invalidationPattern(tenantId, basePath));

  // Make request and cache response
  return next(req).pipe(
    tap((event) => {
      const raced =
        cacheService.generation(invalidationPattern(tenantId, basePath)) !== generationAtRequest;

      if (event instanceof HttpResponse && event.ok && !raced) {
        cacheService.set(key, event.clone(), ttl);
      }
    }),
  );
};
