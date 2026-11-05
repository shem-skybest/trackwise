import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const RateAgreementRoutes: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./rate-agreements-list/rate-agreements-list").then((m) => m.RateAgreementsList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Tariff.View },
  },
  {
    path: "add",
    loadComponent: () =>
      import("./rate-agreement-add/rate-agreement-add").then((m) => m.RateAgreementAdd),
    canActivate: [authGuard],
    data: { breadcrumb: "New Agreement", permission: Permission.Tariff.Manage },
  },
  {
    path: "costing",
    loadComponent: () => import("./route-costing/route-costing").then((m) => m.RouteCosting),
    canActivate: [authGuard],
    data: { breadcrumb: "Route Costing", permission: Permission.Tariff.View },
  },
  {
    path: ":id/edit",
    loadComponent: () =>
      import("./rate-agreement-edit/rate-agreement-edit").then((m) => m.RateAgreementEdit),
    canActivate: [authGuard],
    data: { breadcrumb: "Edit Agreement", permission: Permission.Tariff.Manage },
  },
  {
    path: ":id",
    loadComponent: () =>
      import("./rate-agreement-details/rate-agreement-details").then((m) => m.RateAgreementDetails),
    canActivate: [authGuard],
    data: { breadcrumb: "Agreement", permission: Permission.Tariff.View },
  },
];
