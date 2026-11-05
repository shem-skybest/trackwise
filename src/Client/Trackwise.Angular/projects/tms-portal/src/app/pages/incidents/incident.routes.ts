import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

// Not behind the "safety" feature: incidents are part of running the trips, and the Safety module's
// US inspections stay switched off for tenants that don't use them.
export const incidentRoutes: Routes = [
  {
    path: "",
    loadComponent: () => import("./incidents-list/incidents-list").then((m) => m.IncidentsList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Safety.View },
  },
  {
    path: "add",
    loadComponent: () => import("./incident-add/incident-add").then((m) => m.IncidentAdd),
    canActivate: [authGuard],
    data: { breadcrumb: "Report incident", permission: Permission.Safety.Manage },
  },
  {
    path: ":id",
    loadComponent: () =>
      import("./incident-details/incident-details").then((m) => m.IncidentDetails),
    canActivate: [authGuard],
    data: { breadcrumb: "Incident", permission: Permission.Safety.View },
  },
  {
    path: ":id/edit",
    loadComponent: () => import("./incident-edit/incident-edit").then((m) => m.IncidentEdit),
    canActivate: [authGuard],
    data: { breadcrumb: "Edit", permission: Permission.Safety.Manage },
  },
];
