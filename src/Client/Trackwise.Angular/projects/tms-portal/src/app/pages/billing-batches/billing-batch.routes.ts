import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const billingBatchRoutes: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./billing-batches-list/billing-batches-list").then((m) => m.BillingBatchesList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Invoice.View },
  },
  {
    path: ":id",
    loadComponent: () =>
      import("./billing-batch-details/billing-batch-details").then((m) => m.BillingBatchDetails),
    canActivate: [authGuard],
    data: { breadcrumb: "Batch", permission: Permission.Invoice.View },
  },
];
