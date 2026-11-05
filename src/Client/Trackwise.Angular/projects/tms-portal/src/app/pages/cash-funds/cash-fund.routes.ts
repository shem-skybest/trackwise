import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const cashFundRoutes: Routes = [
  {
    path: "",
    loadComponent: () => import("./cash-funds-list/cash-funds-list").then((m) => m.CashFundsList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Expense.View },
  },
  {
    path: ":id",
    loadComponent: () =>
      import("./cash-fund-details/cash-fund-details").then((m) => m.CashFundDetails),
    canActivate: [authGuard],
    data: { breadcrumb: "Fund", permission: Permission.Expense.View },
  },
];
