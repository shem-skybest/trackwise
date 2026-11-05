import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { HelperAddComponent } from "./helper-add/helper-add";
import { HelperEditComponent } from "./helper-edit/helper-edit";
import { HelpersListComponent } from "./helpers-list/helpers-list";

export const HelperRoutes: Routes = [
  {
    path: "",
    component: HelpersListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: HelperAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Helper",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: HelperEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Helper",
      permission: "Permission.MasterData.Manage",
    },
  },
];
