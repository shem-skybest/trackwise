import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";
import { TripAddPage } from "./trip-add/trip-add";
import { TripDetailsPage } from "./trip-details/trip-details";
import { TripEditPage } from "./trip-edit/trip-edit";
import { TripReportPage } from "./trip-report/trip-report";
import { TripsList } from "./trips-list/trips-list";

export const tripRoutes: Routes = [
  {
    path: "",
    component: TripsList,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Trips",
      permission: Permission.Load.View,
    },
  },
  {
    path: "add",
    component: TripAddPage,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Trip",
      permission: Permission.Load.Manage,
    },
  },
  {
    path: ":tripId/edit",
    component: TripEditPage,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Trip",
      permission: Permission.Load.Manage,
    },
  },
  {
    path: ":tripId/report",
    component: TripReportPage,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Crew Report",
      permission: Permission.Load.Manage,
    },
  },
  {
    path: ":tripId",
    component: TripDetailsPage,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Trip Details",
      permission: Permission.Load.View,
    },
  },
];
