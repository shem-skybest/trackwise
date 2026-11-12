import { Component, computed, inject } from "@angular/core";
import { Permission } from "@trackwise/shared";
import { Container, Stack } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { TenantService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { StripeConnectCard, SubscriptionCard, TenantTaxRatesCard, XenditCard } from "../components";

/** Plan, online payments (Xendit, Stripe Connect) and tax rates in one tab. */
@Component({
  selector: "app-billing-settings",
  templateUrl: "./billing-settings.html",
  imports: [
    Container,
    PageHeader,
    Stack,
    StripeConnectCard,
    SubscriptionCard,
    TenantTaxRatesCard,
    XenditCard,
  ],
})
export class BillingSettings {
  private readonly tenantService = inject(TenantService);

  protected readonly canViewTax = inject(PermissionService).hasPermission(Permission.Tax.View);

  /**
   * A company that doesn't need a TrackWise plan (Skybest runs its own deployment) has nothing to
   * show here but a "No Active Subscription" warning - unless it somehow has one anyway.
   */
  protected readonly showSubscription = computed(() => {
    const tenant = this.tenantService.tenantData();
    return tenant?.isSubscriptionRequired !== false || tenant.subscription != null;
  });
}
