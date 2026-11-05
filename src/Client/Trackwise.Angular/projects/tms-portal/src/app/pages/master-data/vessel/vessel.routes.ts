import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { VesselAddComponent } from "./vessel-add/vessel-add";
import { VesselEditComponent } from "./vessel-edit/vessel-edit";
import { VesselsListComponent } from "./vessels-list/vessels-list";

export const VesselRoutes: Routes = [
  {
    path: "",
    component: VesselsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: VesselAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Vessel",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    path: ":id",
    component: VesselEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Vessel",
      permission: "Permission.MasterData.Manage",
    },
  },
];
