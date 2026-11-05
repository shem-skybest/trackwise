import { UserRole } from "@trackwise/shared";
import { createRoleGuard } from "@trackwise/shared/auth";

// Only app roles can access Admin Portal
export const authGuard = createRoleGuard({
  allowedRoles: [UserRole.AppSuperAdmin, UserRole.AppAdmin],
  unauthenticatedRedirect: "/",
});
