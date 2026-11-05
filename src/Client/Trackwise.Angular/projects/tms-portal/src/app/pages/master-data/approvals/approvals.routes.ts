import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { ApprovalsListComponent } from "./approvals-list/approvals-list";

export const ApprovalsRoutes: Routes = [
  {
    path: "",
    component: ApprovalsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      // Seeing what is in flight is not the same as being able to decide it - the decision routes
      // are gated on Permission.MasterData.Approve on the record's own page and at the API.
      permission: "Permission.MasterData.View",
    },
  },
];
