import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { OidcSecurityService, PublicEventsService } from "angular-auth-oidc-client";
import { of } from "rxjs";
import { vi } from "vitest";
import { HttpCacheService, PermissionService } from "../services";
import { AuthService } from "./auth.service";

/**
 * Guards the fix for the "cache never cleared on logout" bug: cached response bodies used to
 * outlive the session that fetched them, which is a privacy issue on a shared/kiosk browser and,
 * before the cacheInterceptor tenant-ordering fix, a cross-tenant one too.
 */
describe("AuthService", () => {
  let service: AuthService;
  let httpCacheService: { clear: ReturnType<typeof vi.fn> };
  let permissionService: { clearPermissions: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    httpCacheService = { clear: vi.fn() };
    permissionService = { clearPermissions: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: OidcSecurityService,
          useValue: {
            userData$: of({ userData: null }),
            logoff: () => of(undefined),
          },
        },
        { provide: PublicEventsService, useValue: { registerForEvents: () => of() } },
        { provide: PermissionService, useValue: permissionService },
        { provide: HttpCacheService, useValue: httpCacheService },
      ],
    });

    service = TestBed.inject(AuthService);
  });

  it("clears the HTTP cache on logout, alongside permissions", () => {
    service.logout();

    expect(httpCacheService.clear).toHaveBeenCalled();
    expect(permissionService.clearPermissions).toHaveBeenCalled();
  });
});
