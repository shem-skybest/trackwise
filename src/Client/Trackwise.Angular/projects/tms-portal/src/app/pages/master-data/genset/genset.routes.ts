import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { GensetAddComponent } from "./genset-add/genset-add";
import { GensetEditComponent } from "./genset-edit/genset-edit";
import { GensetsListComponent } from "./gensets-list/gensets-list";

export const GensetRoutes: Routes = [
  {
    path: "",
    component: GensetsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: GensetAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Genset",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: GensetEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Genset",
      permission: "Permission.MasterData.Manage",
    },
  },
];
