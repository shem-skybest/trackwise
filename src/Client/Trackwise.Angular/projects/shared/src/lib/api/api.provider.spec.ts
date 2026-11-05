import { HttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { provideApi } from "./api.provider";
import { TENANT_ID_PROVIDER, tenantInterceptor, type TenantIdProvider } from "./interceptors";

const DEV = "http://localhost:7000";

/**
 * Guards the interceptor ordering fix: `cacheInterceptor` builds its key off the `X-Tenant`
 * header, and used to run BEFORE the portal-supplied `tenantInterceptor` that sets it - every
 * cache key got an empty tenant prefix, so one tenant's cached GET could be served to another.
 * This wires the two together the same way tms-portal/customer-portal do (via `provideApi`'s
 * `interceptors` option) rather than unit-testing `cacheInterceptor` alone, since the bug was in
 * the composition, not either interceptor's own logic.
 */
describe("provideApi (tenant/cache interceptor ordering)", () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let currentTenant: string;

  beforeEach(() => {
    currentTenant = "tenant-a";
    const tenantIdProvider: TenantIdProvider = { getTenantId: () => currentTenant };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideApi({ baseUrl: DEV, interceptors: [tenantInterceptor] }),
        provideHttpClientTesting(),
        { provide: TENANT_ID_PROVIDER, useValue: tenantIdProvider },
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it("does not serve one tenant's cached response to another tenant", () => {
    let firstReceived: unknown;
    http.get(`${DEV}/bookings`).subscribe((r) => (firstReceived = r));
    backend.expectOne(`${DEV}/bookings`).flush({ items: ["tenant-a-data"] });
    expect(firstReceived).toEqual({ items: ["tenant-a-data"] });

    currentTenant = "tenant-b";
    let secondReceived: unknown;
    http.get(`${DEV}/bookings`).subscribe((r) => (secondReceived = r));
    // Must reach the backend, not be served from tenant-a's cache entry.
    backend.expectOne(`${DEV}/bookings`).flush({ items: ["tenant-b-data"] });
    expect(secondReceived).toEqual({ items: ["tenant-b-data"] });
  });

  it("still caches within the same tenant", () => {
    http.get(`${DEV}/bookings`).subscribe();
    backend.expectOne(`${DEV}/bookings`).flush({ items: ["tenant-a-data"] });

    let secondReceived: unknown;
    http.get(`${DEV}/bookings`).subscribe((r) => (secondReceived = r));
    backend.expectNone(`${DEV}/bookings`);
    expect(secondReceived).toEqual({ items: ["tenant-a-data"] });
  });
});
