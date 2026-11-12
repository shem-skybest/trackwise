import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { isTenantStaffRole } from "@trackwise/shared";
import { createRoleGuard } from "@trackwise/shared/auth";
import { TenantService } from "../services";

const subscriptionCheck = () =>
  inject(TenantService).isSubscriptionActive()
    ? true
    : inject(Router).parseUrl("/unauthorized?reason=subscription");

// Any company staff role can open the TMS Portal - built-in or created in Settings - but not app
// roles or portal customers. What each person then sees is decided by `data.permission` per route,
// which `createRoleGuard` checks automatically.
export const authGuard = createRoleGuard({
  allowRole: isTenantStaffRole,
  unauthenticatedRedirect: "/",
  extraCheck: subscriptionCheck,
});

// Tenant administration. Every tab carries its own `data.permission` (Tenant.View, Payment.View,
// TenantRole.Manage...), so the role check is the same as the portal's and the tabs do the rest.
export const settingsGuard = createRoleGuard({
  allowRole: isTenantStaffRole,
  unauthenticatedRedirect: "/",
  extraCheck: subscriptionCheck,
});
