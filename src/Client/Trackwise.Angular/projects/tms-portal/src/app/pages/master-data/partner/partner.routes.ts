import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { PartnerAddComponent } from "./partner-add/partner-add";
import { PartnerEditComponent } from "./partner-edit/partner-edit";
import { PartnersListComponent } from "./partners-list/partners-list";

export const PartnerRoutes: Routes = [
  {
    path: "",
    component: PartnersListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.MasterData.View",
    },
  },
  {
    path: "add",
    component: PartnerAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Business Partner",
      permission: "Permission.MasterData.Manage",
    },
  },
  {
    // View rather than Manage: an approver opens this page to decide a submission, and the approve
    // and send-back routes are gated on Permission.MasterData.Approve server-side. Gating the page
    // on Manage would hide the queue from the one person it exists for.
    path: ":id",
    component: PartnerEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Edit Business Partner",
      permission: "Permission.MasterData.View",
    },
  },
];
