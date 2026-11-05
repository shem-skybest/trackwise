import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { TrailerAddComponent } from "./trailer-add/trailer-add";
import { TrailerEditComponent } from "./trailer-edit/trailer-edit";
import { TrailersListComponent } from "./trailers-list/trailers-list";

export const TrailerRoutes: Routes = [
  {
    path: "",
    component: TrailersListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: TrailerAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Trailer",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: TrailerEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Trailer",
      permission: "Permission.MasterData.Manage",
    },
  },
];
