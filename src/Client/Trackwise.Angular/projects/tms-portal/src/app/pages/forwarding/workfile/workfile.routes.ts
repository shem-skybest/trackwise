import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { WorkfileDetailsComponent } from "./workfile-details/workfile-details";
import { WorkfilesListComponent } from "./workfiles-list/workfiles-list";

export const WorkfileRoutes: Routes = [
  {
    path: "",
    component: WorkfilesListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.Workfile.View",
    },
  },
  {
    path: ":id",
    component: WorkfileDetailsComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Workfile Details",
      permission: "Permission.Workfile.View",
    },
  },
];
