import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { TariffAddComponent } from "./tariff-add/tariff-add";
import { TariffChargeCodeAdd } from "./tariff-charge-code-add/tariff-charge-code-add";
import { TariffDetails } from "./tariff-details/tariff-details";
import { TariffEditComponent } from "./tariff-edit/tariff-edit";
import { TariffRateAdd } from "./tariff-rate-add/tariff-rate-add";
import { TariffsListComponent } from "./tariffs-list/tariffs-list";

export const TariffRoutes: Routes = [
  {
    path: "",
    component: TariffsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.Tariff.View",
    },
  },
  {
    path: "add",
    component: TariffAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Tariff",
      permission: "Permission.Tariff.Manage",
    },
  },
  {
    path: ":tariffId/charge-codes/add",
    component: TariffChargeCodeAdd,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Charge Code",
      permission: "Permission.Tariff.Manage",
    },
  },
  {
    path: ":id/rates/add",
    component: TariffRateAdd,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Rate",
      permission: "Permission.Tariff.Manage",
    },
  },
  {
    path: "edit/:id",
    component: TariffEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Tariff",
      permission: "Permission.Tariff.Manage",
    },
  },
  {
    path: ":id",
    component: TariffDetails,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Tariff Details",
      permission: "Permission.Tariff.View",
    },
  },
];
