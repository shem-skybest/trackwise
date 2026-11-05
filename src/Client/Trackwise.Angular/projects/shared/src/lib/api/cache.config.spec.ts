/**
 * Cache ROUTING POLICY spec - `getBasePath` decides what a write evicts, `getTtlForUrl` decides
 * what gets cached at all.
 *
 * The regression this locks down: `getBasePath` used to look for a literal `/api/` path segment,
 * which no URL in this app has. Dev is `http://localhost:7000` and production is
 * `https://api.skybest.com.ph` - where `api.` is a SUBDOMAIN, followed by `.` and not `/`. The
 * match never fired, the whole mutation URL was returned as the "base path", and invalidation
 * silently evicted nothing in any module for as long as the cache has existed.
 *
 * The prod-origin cases below are therefore not padding - they are the bug.
 */
import { getBasePath, getInvalidationTargets, getTtlForUrl } from "./cache.config";

const DEV = "http://localhost:7000";
const PROD = "https://api.skybest.com.ph";
const GUID = "3f8e1b2c-0000-4000-8000-000000000001";

describe("getBasePath", () => {
  it("reads the resource segment from a dev-origin URL", () => {
    expect(getBasePath(`${DEV}/bookings`)).toBe("/bookings");
  });

  it("ignores sub-resources and actions", () => {
    expect(getBasePath(`${DEV}/bookings/${GUID}/confirm`)).toBe("/bookings");
  });

  it("ignores query parameters", () => {
    expect(getBasePath(`${DEV}/bookings?Page=1&PageSize=10`)).toBe("/bookings");
  });

  it("ignores a fragment", () => {
    expect(getBasePath(`${DEV}/loads#section`)).toBe("/loads");
  });

  // THE REGRESSION: `api.` here is a subdomain, not a path segment.
  it("reads the resource segment from the production origin", () => {
    expect(getBasePath(`${PROD}/loads/123`)).toBe("/loads");
  });

  it("still strips a genuine /api path prefix, for a future deployment that uses one", () => {
    expect(getBasePath(`${PROD}/api/loads/123`)).toBe("/loads");
  });

  it("does not collapse a bare /api request to the root", () => {
    expect(getBasePath(`${DEV}/api`)).toBe("/api");
  });

  it("handles relative URLs", () => {
    expect(getBasePath(`/quotations/${GUID}/convert-to-booking`)).toBe("/quotations");
  });

  it("handles a relative URL with no leading slash", () => {
    expect(getBasePath("quotations/1")).toBe("/quotations");
  });

  it("returns the root for an origin with no path", () => {
    expect(getBasePath(`${DEV}/`)).toBe("/");
  });

  it("does not throw on malformed input", () => {
    expect(() => getBasePath("::::")).not.toThrow();
  });
});

describe("getTtlForUrl", () => {
  it("does not cache real-time surfaces", () => {
    expect(getTtlForUrl(`${DEV}/ai/dispatch/conversations`)).toBe(0);
    expect(getTtlForUrl(`${DEV}/documents`)).toBe(0);
  });

  it("keeps the moderate and long TTLs", () => {
    expect(getTtlForUrl(`${DEV}/customers`)).toBe(5 * 60 * 1000);
    expect(getTtlForUrl(`${DEV}/settings`)).toBe(30 * 60 * 1000);
  });

  /**
   * Deliberate: bookings are cached on the 2-min catch-all like every other resource. A `ttl: 0`
   * rule for `/bookings` would be the band-aid that hides broken invalidation rather than fixing
   * it - which is exactly how this bug survived. If this test fails, the band-aid came back.
   */
  it("leaves /bookings on the default catch-all rather than opting it out of caching", () => {
    expect(getTtlForUrl(`${DEV}/bookings`)).toBe(2 * 60 * 1000);
  });

  it("matches on the pathname, so a query value cannot force a rule", () => {
    expect(getTtlForUrl(`${DEV}/bookings?search=/documents`)).toBe(2 * 60 * 1000);
  });
});

describe("getInvalidationTargets", () => {
  it("returns just the resource's own base path for an ordinary mutation", () => {
    expect(getInvalidationTargets(`${DEV}/bookings/${GUID}/confirm`)).toEqual(["/bookings"]);
  });

  it("also evicts bookings when a quotation is converted into one", () => {
    expect(getInvalidationTargets(`${DEV}/quotations/${GUID}/convert-to-booking`)).toEqual([
      "/quotations",
      "/bookings",
    ]);
  });

  it("also evicts loads when a load board listing is booked", () => {
    expect(getInvalidationTargets(`${DEV}/loadboard/listings/${GUID}/book`)).toEqual([
      "/loadboard",
      "/loads",
    ]);
  });

  it("also evicts trips and loads when a booking's trip is made", () => {
    expect(getInvalidationTargets(`${DEV}/bookings/${GUID}/trip`)).toEqual([
      "/bookings",
      "/trips",
      "/loads",
    ]);
  });

  it("also evicts bookings when a trip is cancelled or its last drop departs", () => {
    expect(getInvalidationTargets(`${DEV}/trips/${GUID}/cancel`)).toEqual(["/trips", "/bookings"]);
    expect(getInvalidationTargets(`${DEV}/trips/${GUID}/stops/${GUID}/depart`)).toEqual([
      "/trips",
      "/bookings",
    ]);
    expect(getInvalidationTargets(`${DEV}/trips/${GUID}/stops/${GUID}/arrive`)).toEqual(["/trips"]);
  });

  // The cross-resource patterns are anchored so an ordinary write to the same resource cannot
  // pick them up and fan out.
  it("does not fan out for other mutations on the same resource", () => {
    expect(getInvalidationTargets(`${DEV}/quotations/${GUID}`)).toEqual(["/quotations"]);
    expect(getInvalidationTargets(`${DEV}/quotations/${GUID}/approve`)).toEqual(["/quotations"]);
  });

  it("de-duplicates targets", () => {
    const targets = getInvalidationTargets(`${DEV}/quotations/${GUID}/convert-to-booking`);
    expect(new Set(targets).size).toBe(targets.length);
  });
});
