import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { QuotationAddComponent } from "./quotation-add/quotation-add";
import { QuotationDetailsComponent } from "./quotation-details/quotation-details";
import { QuotationEditComponent } from "./quotation-edit/quotation-edit";
import { QuotationsListComponent } from "./quotations-list/quotations-list";

export const QuotationRoutes: Routes = [
  {
    path: "",
    component: QuotationsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.Quotation.View",
    },
  },
  {
    path: "add",
    component: QuotationAddComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add Quotation",
      permission: "Permission.Quotation.Manage",
    },
  },
  {
    path: ":id",
    component: QuotationDetailsComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Quotation Details",
      permission: "Permission.Quotation.View",
    },
  },
  {
    path: ":id/edit",
    component: QuotationEditComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Quotation Edit",
      permission: "Permission.Quotation.Manage",
    },
  },
];
