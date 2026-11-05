interface CacheRule {
  pattern: RegExp;
  ttl: number; // milliseconds, 0 = no cache
}

/**
 * Predefined cache rules for different API endpoints.
 * Rules are evaluated in order - first match wins.
 */
const cacheRules: CacheRule[] = [
  // No caching for frequently mutated data
  { pattern: /\/ai\/dispatch/, ttl: 0 }, // AI dispatch - real-time updates via SignalR
  { pattern: /\/ai\/copilot/, ttl: 0 }, // AI copilot - transcript updated via SignalR mid-conversation
  { pattern: /\/negotiations/, ttl: 0 }, // Broker threads move via SignalR while the page is open
  { pattern: /\/documents/, ttl: 0 }, // Documents change often, don't cache
  { pattern: /\/messages/, ttl: 0 }, // Real-time data, don't cache
  { pattern: /\/invoices/, ttl: 0 }, // Invoices change often with payments, don't cache
  { pattern: /\/privacy/, ttl: 0 }, // Export status polled while job runs; signed URL must be fresh
  { pattern: /\/onboarding/, ttl: 0 }, // Fan-in read model: reflects writes to trucks/loads/customers/employees. A cross-resource rule per contributing path would be exactly the fan-out this file forbids, so it opts out of caching instead
  { pattern: /\/workfiles/, ttl: 0 }, // A file moves between ops, a manager and Accounting; a stale copy offers the wrong actions to whoever opens it next

  // Reference/static data - longer TTL
  { pattern: /\/settings/, ttl: 30 * 60 * 1000 }, // 30 min
  { pattern: /\/roles/, ttl: 30 * 60 * 1000 }, // 30 min

  // Frequently accessed lists - moderate TTL
  { pattern: /\/employees/, ttl: 5 * 60 * 1000 }, // 5 min
  { pattern: /\/customers/, ttl: 5 * 60 * 1000 }, // 5 min
  { pattern: /\/trucks/, ttl: 5 * 60 * 1000 }, // 5 min
  { pattern: /\/drivers/, ttl: 5 * 60 * 1000 }, // 5 min
  { pattern: /\/business-partners/, ttl: 5 * 60 * 1000 }, // 5 min - counterparty master, edited rarely

  // Default for other GET requests
  { pattern: /.*/, ttl: 2 * 60 * 1000 }, // 2 min default
];

/** Path segments a deployment may sit behind, stripped before the resource segment is read. */
const PATH_PREFIXES = new Set(["api"]);

/**
 * Reduces any request URL - absolute or relative - to its pathname.
 *
 * Parsing rather than pattern-matching is deliberate: a regex has to encode where the origin ends
 * and the path begins, and that ambiguity is what previously broke invalidation (the production
 * host is `api.skybest.com.ph`, where `api.` is a subdomain, not an `/api/` path segment).
 */
function toPathname(url: string): string {
  try {
    // The base is consulted only for relative URLs; absolute ones ignore it entirely.
    return new URL(url, "http://relative.invalid").pathname;
  } catch {
    return url.split(/[?#]/)[0];
  }
}

/**
 * The leading resource segment of a request URL, e.g. `/bookings` for
 * `https://api.skybest.com.ph/bookings/{id}/confirm`. Cache entries are keyed and invalidated by
 * this, so a write to any endpoint under a resource clears every cached read of it.
 */
export function getBasePath(url: string): string {
  const segments = toPathname(url).split("/").filter(Boolean);

  // Guard against a bare `/api` request collapsing to `/`.
  if (segments.length > 1 && PATH_PREFIXES.has(segments[0])) {
    segments.shift();
  }

  return segments.length > 0 ? `/${segments[0]}` : "/";
}

interface CrossResourceRule {
  /** Anchored against the mutation's pathname so a rule cannot silently widen. */
  path: RegExp;
  /** Base paths to evict IN ADDITION to the mutation's own. */
  alsoInvalidates: string[];
}

/**
 * Mutations that materialise an entity under a _different_ base path, and so must evict more than
 * their own. Unlike `cacheRules` this is a fan-out: every matching rule applies.
 *
 * Before adding an entry:
 * 1. It must CREATE or DELETE an entity under another base path. Merely refreshing a derived read
 *    model (`/stats`, `/reports`, `/onboarding`) is a TTL decision, not an invalidation rule -
 *    otherwise one write fans out to a dozen entries.
 * 2. Anchor the pattern and name the entity it creates.
 * 3. Cover it in `cache.config.spec.ts`. An unasserted rule is one nobody will dare delete later.
 */
const crossResourceRules: CrossResourceRule[] = [
  // Converting an approved quotation creates a Booking.
  { path: /^\/quotations\/[^/]+\/convert-to-booking$/, alsoInvalidates: ["/bookings"] },
  // Booking a load board listing creates a Load.
  { path: /^\/loadboard\/listings\/[^/]+\/book$/, alsoInvalidates: ["/loads"] },
  // Making a Cold Chain booking's trip creates the trip and one load per drop.
  { path: /^\/bookings\/[^/]+\/trip$/, alsoInvalidates: ["/trips", "/loads"] },
  // Cancelling a trip reopens its Cold Chain booking; the last drop's departure (or the office
  // trip report) completes the trip and with it the booking.
  {
    path: /^\/trips\/[^/]+\/(cancel|report|stops\/[^/]+\/depart)$/,
    alsoInvalidates: ["/bookings"],
  },
];

/** Every base path a mutation to this URL should evict, its own included. */
export function getInvalidationTargets(url: string): string[] {
  const pathname = toPathname(url);
  const extra = crossResourceRules
    .filter((rule) => rule.path.test(pathname))
    .flatMap((rule) => rule.alsoInvalidates);

  return [...new Set([getBasePath(url), ...extra])];
}

export function getTtlForUrl(url: string): number | null {
  const pathname = toPathname(url);
  for (const rule of cacheRules) {
    if (rule.pattern.test(pathname)) {
      return rule.ttl;
    }
  }
  return null;
}
