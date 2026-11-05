import { HttpContext } from "@angular/common/http";
import { computed, inject, Injectable, signal } from "@angular/core";
import { CookieService } from "@trackwise/shared";
import {
  Api,
  getTenantById,
  SKIP_CACHE,
  type OperatingMode,
  type TenantDto,
} from "@trackwise/shared/api";

@Injectable({ providedIn: "root" })
export class TenantService {
  private readonly cookieService = inject(CookieService);
  private readonly api = inject(Api);

  private readonly tenantId = signal<string | null>(null);
  private readonly _tenantData = signal<TenantDto | null>(null);

  /**
   * Signal that holds the current tenant data.
   * Use effect() to react to changes.
   */
  public readonly tenantData = this._tenantData.asReadonly();

  /**
   * Signal that exposes the current tenant's currency setting.
   * Defaults to 'USD' if not set.
   */
  public readonly tenantCurrency = computed(() => this._tenantData()?.settings?.currency ?? "USD");

  /** The tenant's operating mode. Single source for every solo-vs-fleet branch in the portal. */
  public readonly operatingMode = computed<OperatingMode>(
    () => this._tenantData()?.settings?.operatingMode ?? "fleet",
  );

  public readonly isSoloMode = computed(() => this.operatingMode() === "solo_operator");

  /** Cold Chain booking numbers are typed in from nSFS rather than issued by TrackWise. */
  public readonly coldChainBookingNumbersTyped = computed(
    () => this._tenantData()?.settings?.coldChainBookingNumbersTyped ?? false,
  );

  getTenantData(): TenantDto | null {
    return this._tenantData();
  }

  /**
   * Set tenant id and save it to the cookie, then fetch tenant data
   * @param tenantId Tenant ID
   */
  setTenantId(tenantId: string): void {
    this.tenantId.set(tenantId);
    this.setTenantCookie(tenantId);
    this.fetchTenantData(tenantId);
  }

  /**
   * Get tenant id
   */
  getTenantId(): string | null {
    return this.tenantId();
  }

  /**
   * Check if the tenant has an active subscription
   * If the tenant is not required to have a subscription, it returns true
   * @returns True if the tenant has an active subscription, otherwise false
   */
  isSubscriptionActive(): boolean {
    const data = this._tenantData();
    if (!data) {
      return false;
    }

    // If subscription is null, it means the tenant is not required to have a subscription
    if (data.subscription == null) {
      return true;
    }

    return data.subscription.status === "active" || data.subscription.status === "trialing";
  }

  /**
   * Refetch tenant data from the server, past the HTTP cache: a setting saved under another
   * resource (e.g. Cold Chain booking numbers, under /bookings) doesn't invalidate /tenants.
   * Updates the tenantData signal with fresh data.
   */
  public refetchTenantData(): void {
    const tenantId = this.tenantId();
    if (!tenantId) {
      return;
    }

    this.fetchTenantData(tenantId, true);
  }

  private setTenantCookie(tenantId: string): void {
    if (!tenantId) {
      return;
    }

    const currentTenant = this.cookieService.getCookie("X-Tenant");

    if (tenantId === currentTenant) {
      return;
    }

    this.cookieService.setCookie({
      name: "X-Tenant",
      value: tenantId,
      session: true,
    });
  }

  /**
   * Fetch tenant data and save it to the tenantData signal
   * @param tenantId The tenant ID
   * @param fresh Skip the HTTP cache
   */
  private async fetchTenantData(tenantId: string, fresh = false): Promise<void> {
    const tenant = await this.api.invoke(
      getTenantById,
      { identifier: tenantId },
      fresh ? new HttpContext().set(SKIP_CACHE, true) : undefined,
    );

    if (!tenant) {
      return;
    }

    this._tenantData.set(tenant);
    console.log("Fetched tenant data:", tenant);
  }
}
