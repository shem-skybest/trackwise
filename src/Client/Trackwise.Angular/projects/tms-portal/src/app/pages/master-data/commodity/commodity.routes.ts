import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { CommoditiesListComponent } from "./commodities-list/commodities-list";
import { CommodityAddComponent } from "./commodity-add/commodity-add";
import { CommodityEditComponent } from "./commodity-edit/commodity-edit";

export const CommodityRoutes: Routes = [
  {
    path: "",
    component: CommoditiesListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: CommodityAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Commodity",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: CommodityEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Commodity",
      permission: "Permission.MasterData.Manage",
    },
  },
];
