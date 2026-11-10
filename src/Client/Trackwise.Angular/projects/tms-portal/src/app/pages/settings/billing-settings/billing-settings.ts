import { Component, inject } from "@angular/core";
import { Permission } from "@trackwise/shared";
import { Container, Stack } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { PageHeader } from "@/shared/components";
import { StripeConnectCard, SubscriptionCard, TenantTaxRatesCard } from "../components";

/** Plan, payouts and tax rates in one tab. */
@Component({
  selector: "app-billing-settings",
  templateUrl: "./billing-settings.html",
  imports: [Container, PageHeader, Stack, StripeConnectCard, SubscriptionCard, TenantTaxRatesCard],
})
export class BillingSettings {
  protected readonly canViewTax = inject(PermissionService).hasPermission(Permission.Tax.View);
}
