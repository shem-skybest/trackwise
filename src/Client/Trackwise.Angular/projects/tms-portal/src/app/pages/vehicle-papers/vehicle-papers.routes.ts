import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";

export const vehiclePapersRoutes: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./vehicle-papers-list/vehicle-papers-list").then((m) => m.VehiclePapersList),
    canActivate: [authGuard],
    data: { breadcrumb: "", permission: Permission.Truck.View },
  },
];
