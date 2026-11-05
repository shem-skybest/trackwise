import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { ContainerTypeAddComponent } from "./container-type-add/container-type-add";
import { ContainerTypeEditComponent } from "./container-type-edit/container-type-edit";
import { ContainerTypesListComponent } from "./container-types-list/container-types-list";

export const ContainerTypeRoutes: Routes = [
  {
    path: "",
    component: ContainerTypesListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: ContainerTypeAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Container Type",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: ContainerTypeEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Container Type",
      permission: "Permission.MasterData.Manage",
    },
  },
];
