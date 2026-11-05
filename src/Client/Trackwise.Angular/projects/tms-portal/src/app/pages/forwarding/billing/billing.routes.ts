import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { BillingDetailsComponent } from "./billing-details/billing-details";
import { BillingListComponent } from "./billing-list/billing-list.component";

export const BillingRoutes: Routes = [
  {
    path: "",
    component: BillingListComponent,
    // canActivate: [authGuard],
    data: {
      breadcrumb: "Billing & Invoicing",
      permission: "Permission.Billing.View",
    },
  },
  {
    path: ":id",
    component: BillingDetailsComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Billing Details",
      permission: "Permission.Billing.View",
    },
  },
];
