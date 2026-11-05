import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { CurrenciesListComponent } from "./currencies-list/currencies-list";
import { CurrencyAddComponent } from "./currency-add/currency-add";
import { CurrencyEditComponent } from "./currency-edit/currency-edit";

export const CurrencyRoutes: Routes = [
  {
    path: "",
    component: CurrenciesListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: CurrencyAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Currency",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: CurrencyEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Currency",
      permission: "Permission.MasterData.Manage",
    },
  },
];
