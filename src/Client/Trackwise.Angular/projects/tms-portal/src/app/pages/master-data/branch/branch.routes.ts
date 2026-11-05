import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { BranchAddComponent } from "./branch-add/branch-add";
import { BranchEditComponent } from "./branch-edit/branch-edit";
import { BranchesListComponent } from "./branches-list/branches-list";

export const BranchRoutes: Routes = [
  {
    path: "",
    component: BranchesListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: BranchAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Branch",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: BranchEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Branch",
      permission: "Permission.MasterData.Manage",
    },
  },
];
