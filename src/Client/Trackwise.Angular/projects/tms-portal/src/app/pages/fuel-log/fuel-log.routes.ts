import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const fuelLogRoutes: Routes = [
  {
    path: "",
    loadComponent: () => import("./fuel-log-list/fuel-log-list").then((m) => m.FuelLogList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Expense.View },
  },
];
