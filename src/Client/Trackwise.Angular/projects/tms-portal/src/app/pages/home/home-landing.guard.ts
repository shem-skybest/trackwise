import { inject, Injector } from "@angular/core";
import { toObservable } from "@angular/core/rxjs-interop";
import { Router, type CanActivateFn } from "@angular/router";
import { Permission } from "@trackwise/shared";
import { filter, map, take } from "rxjs";
import { AuthService, PermissionService } from "@/core/auth";
import { SidebarNavService } from "@/core/services";
import type { NavItem } from "@/shared/layout/nav-menu";

/**
 * Home is the operations dashboard - loads, trucks, gross - and every sign-in lands on it. A role
 * with no access to loads (HR, Admin Staff) would get "Access Denied" the moment they sign in, so
 * send them to the first page their sidebar offers instead.
 *
 * Waits for auth to settle on its own: Angular starts every `canActivate` guard at once, so on a
 * page reload this would otherwise read permissions before `authGuard` has loaded them.
 */
export const homeLandingGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const permissions = inject(PermissionService);
  const sidebar = inject(SidebarNavService);
  const router = inject(Router);

  return toObservable(auth.authInitialized, { injector: inject(Injector) }).pipe(
    filter(Boolean),
    take(1),
    map(() => {
      if (permissions.hasPermission(Permission.Load.View)) {
        return true;
      }

      const firstRoute = sidebar
        .menuSections()
        .flatMap((section) => section.items)
        .map(firstReachableRoute)
        .find((route): route is string => !!route && route !== "/home");

      return router.parseUrl(firstRoute ?? "/unauthorized");
    }),
  );
};

function firstReachableRoute(item: NavItem): string | undefined {
  return item.route ?? item.children?.find((child) => child.route)?.route;
}
