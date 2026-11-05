import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { PortAddComponent } from "./port-add/port-add";
import { PortEditComponent } from "./port-edit/port-edit";
import { PortsListComponent } from "./ports-list/ports-list";

export const PortRoutes: Routes = [
  {
    path: "",
    component: PortsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: PortAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Port",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: PortEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Port",
      permission: "Permission.MasterData.Manage",
    },
  },
];
