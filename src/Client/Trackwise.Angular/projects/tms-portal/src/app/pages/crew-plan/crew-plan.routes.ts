import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const crewPlanRoutes: Routes = [
  {
    path: "",
    loadComponent: () => import("./crew-plan-list/crew-plan-list").then((m) => m.CrewPlanList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Load.View },
  },
];
