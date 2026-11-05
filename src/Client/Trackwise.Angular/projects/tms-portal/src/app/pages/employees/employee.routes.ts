import type { Routes } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { authGuard } from "@/core/auth";
import { EmployeeAdd } from "./employee-add/employee-add";
import { EmployeeDetails } from "./employee-details/employee-details";
import { EmployeeList } from "./employees-list/employees-list";
import { PendingInvitations } from "./pending-invitations/pending-invitations";

export const employeeRoutes: Routes = [
  {
    path: "",
    component: EmployeeList,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: Permission.MasterData.View,
    },
  },
  {
    path: "add",
    component: EmployeeAdd,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Add",
      // Writes go through /master-data/employees - see EmployeeController.cs (MasterData) vs the
      // trimmed, read-only EmployeeController.cs. Permission.Employee.Manage is unused server-side
      // now, matching Truck.Manage's fate.
      permission: Permission.MasterData.Manage,
    },
  },
  {
    path: "invitations",
    component: PendingInvitations,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Invitations",
      // Untouched by this promotion - inviting a new platform user is a separate creation path
      // from editing an existing employee record (see AcceptInvitationHandler).
      permission: Permission.Invitation.Manage,
    },
  },
  {
    path: ":id",
    component: EmployeeDetails,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Details",
      permission: Permission.MasterData.View,
    },
  },
];
