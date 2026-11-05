import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";
import { TruckDetailsComponent as TruckDetails } from "./truck-details/truck-details";
import { TruckDocumentsPage } from "./truck-documents/truck-documents";
import { TruckEdit } from "./truck-edit/truck-edit";
import { TrucksList } from "./trucks-list/trucks-list";

export const truckRoutes: Routes = [
  {
    path: "",
    component: TrucksList,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: Permission.MasterData.View,
    },
  },
  {
    path: "add",
    component: TruckEdit,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add",
      // Writes go through /master-data/trucks - see TruckController.cs (MasterData) vs the
      // trimmed, read-only TruckController.cs. Permission.Truck.Manage is unused server-side now.
      permission: Permission.MasterData.Manage,
    },
  },
  {
    path: ":id/edit",
    component: TruckEdit,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit",
      permission: Permission.MasterData.Manage,
    },
  },
  {
    path: ":id/documents",
    component: TruckDocumentsPage,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Documents",
      permission: Permission.MasterData.View,
    },
  },
  {
    path: ":id",
    component: TruckDetails,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Details",
      permission: Permission.MasterData.View,
    },
  },
];
