/**
 * BEHAVIOUR spec for `cacheInterceptor`, asserted the only way that matters: how many requests
 * actually reach the backend, and which body the caller ends up with.
 *
 * The bug this guards against was invisible from the feature side. Pages did everything right -
 * re-issuing their GET after a write - and were silently handed the pre-write response from an
 * in-memory cache that no mutation could evict, so the UI only updated on a hard refresh.
 *
 * Every assertion here is about the caller's observable experience, never about the cache's
 * internals, so the eviction strategy can be rewritten without rewriting this file.
 */
import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
  type TestRequest,
} from "@angular/common/http/testing";
import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { cacheInterceptor, SKIP_CACHE } from "./cache.interceptor";

const DEV = "http://localhost:7000";
const PROD = "https://api.skybest.com.ph";
const GUID = "3f8e1b2c-0000-4000-8000-000000000001";

describe("cacheInterceptor", () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([cacheInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  /** Issues a GET and flushes it, returning nothing - for priming the cache. */
  function getAndFlush(url: string, body: object): void {
    let received: unknown;
    http.get(url).subscribe((r) => (received = r));
    backend.expectOne(url).flush(body);
    expect(received).toEqual(body);
  }

  /** Issues a GET and returns the pending backend request, or null when it was served from cache. */
  function getExpectingMaybeRequest(
    url: string,
    received: { value?: unknown },
  ): TestRequest | null {
    http.get(url).subscribe((r) => (received.value = r));
    const matches = backend.match(url);
    expect(matches.length).toBeLessThanOrEqual(1);
    return matches[0] ?? null;
  }

  function mutate(method: "POST" | "PUT" | "PATCH" | "DELETE", url: string): void {
    const request =
      method === "DELETE" ? http.delete(url) : http.request(method, url, { body: {} });
    request.subscribe({ next: () => undefined, error: () => undefined });
    backend.expectOne(url).flush({});
  }

  describe("caching", () => {
    it("serves a repeated GET from cache without hitting the backend", () => {
      getAndFlush(`${DEV}/bookings`, { items: [{ id: GUID }] });

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/bookings`, received)).toBeNull();
      expect(received.value).toEqual({ items: [{ id: GUID }] });
    });

    it("never caches a GET marked SKIP_CACHE", () => {
      const url = `${DEV}/employees?AvailableOn=2026-09-30`;
      const context = new HttpContext().set(SKIP_CACHE, true);
      http.get(url, { context }).subscribe();
      backend.expectOne(url).flush({ items: [] });

      http.get(url, { context }).subscribe();
      backend.expectOne(url).flush({ items: [] });
    });

    it("never caches a ttl:0 resource", () => {
      getAndFlush(`${DEV}/documents`, []);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/documents`, received)).not.toBeNull();
      backend.expectNone(`${DEV}/documents`);
    });
  });

  describe("invalidation after a write", () => {
    /**
     * THE REGRESSION, end to end: this is the "confirm a booking and the page still says Pending"
     * report, reduced. On the broken implementation the second GET is served from cache and the
     * caller sees `pending`.
     */
    it("serves fresh data after a mutation on the same resource", () => {
      getAndFlush(`${DEV}/bookings/${GUID}`, { id: GUID, status: "pending" });

      mutate("POST", `${DEV}/bookings/${GUID}/confirm`);

      const received: { value?: unknown } = {};
      const refetch = getExpectingMaybeRequest(`${DEV}/bookings/${GUID}`, received);
      expect(refetch).not.toBeNull();
      refetch!.flush({ id: GUID, status: "confirmed" });
      expect(received.value).toEqual({ id: GUID, status: "confirmed" });
    });

    // Same flow against the production origin, where `api.` is a subdomain - the exact shape that
    // defeated the previous implementation.
    it("works against the production origin", () => {
      getAndFlush(`${PROD}/bookings/${GUID}`, { status: "pending" });

      mutate("POST", `${PROD}/bookings/${GUID}/confirm`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${PROD}/bookings/${GUID}`, received)).not.toBeNull();
      backend.expectNone(`${PROD}/bookings/${GUID}`);
    });

    it("evicts a list cached with query parameters", () => {
      const list = `${DEV}/bookings?Page=1&PageSize=10`;
      getAndFlush(list, { items: [] });

      mutate("POST", `${DEV}/bookings/${GUID}/confirm`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(list, received)).not.toBeNull();
      backend.expectNone(list);
    });

    it("evicts across resources when a quotation is converted into a booking", () => {
      getAndFlush(`${DEV}/bookings`, { items: [] });

      mutate("POST", `${DEV}/quotations/${GUID}/convert-to-booking`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/bookings`, received)).not.toBeNull();
      backend.expectNone(`${DEV}/bookings`);
    });

    /**
     * A failed write still evicts. Deliberate: once a write has been attempted we cannot vouch for
     * what the server holds, and an extra refetch is far cheaper than showing stale data.
     */
    it("evicts even when the mutation fails", () => {
      getAndFlush(`${DEV}/bookings/${GUID}`, { status: "pending" });

      http
        .post(`${DEV}/bookings/${GUID}/confirm`, {})
        .subscribe({ next: () => undefined, error: () => undefined });
      backend
        .expectOne(`${DEV}/bookings/${GUID}/confirm`)
        .flush({}, { status: 500, statusText: "Server Error" });

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/bookings/${GUID}`, received)).not.toBeNull();
      backend.expectNone(`${DEV}/bookings/${GUID}`);
    });

    /**
     * The race the response-time eviction exists for: a GET already in flight when the write was
     * sent lands afterwards and would otherwise repopulate the cache with pre-write data, pinning
     * it for the full TTL.
     */
    it("evicts a GET that lands after the write was sent", () => {
      let firstRead: unknown;
      http.get(`${DEV}/bookings`).subscribe((r) => (firstRead = r));
      const inFlight = backend.expectOne(`${DEV}/bookings`);

      mutate("POST", `${DEV}/bookings/${GUID}/confirm`);

      // The stale read only now comes back and populates the cache.
      inFlight.flush({ items: ["stale"] });
      expect(firstRead).toEqual({ items: ["stale"] });

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/bookings`, received)).not.toBeNull();
      backend.expectNone(`${DEV}/bookings`);
    });
  });

  describe("no over-eviction", () => {
    // `invalidate` matches with `includes`, and `"…/tax-rates".includes("/tax")` is true - both
    // are real endpoints, so the cache key delimits the base path to keep them apart.
    it("a write to /tax does not evict /tax-rates", () => {
      getAndFlush(`${DEV}/tax-rates`, { items: ["kept"] });

      mutate("POST", `${DEV}/tax/jurisdictions`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/tax-rates`, received)).toBeNull();
      expect(received.value).toEqual({ items: ["kept"] });
    });

    it("a write to /loads does not evict /loadboard", () => {
      getAndFlush(`${DEV}/loadboard/search?q=1`, { items: ["kept"] });

      mutate("PUT", `${DEV}/loads/${GUID}`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/loadboard/search?q=1`, received)).toBeNull();
      expect(received.value).toEqual({ items: ["kept"] });
    });

    it("an unrelated resource is untouched", () => {
      getAndFlush(`${DEV}/trucks`, { items: ["kept"] });

      mutate("DELETE", `${DEV}/bookings/${GUID}`);

      const received: { value?: unknown } = {};
      expect(getExpectingMaybeRequest(`${DEV}/trucks`, received)).toBeNull();
      expect(received.value).toEqual({ items: ["kept"] });
    });
  });
});
