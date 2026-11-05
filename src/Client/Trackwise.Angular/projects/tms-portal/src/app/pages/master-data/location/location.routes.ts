import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { LocationAddComponent } from "./location-add/location-add";
import { LocationEditComponent } from "./location-edit/location-edit";
import { LocationsListComponent } from "./locations-list/locations-list";

export const LocationRoutes: Routes = [
  {
    path: "",
    component: LocationsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: LocationAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Location",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: LocationEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Location",
      permission: "Permission.MasterData.Manage",
    },
  },
];
