import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { Home } from "./home";
import { homeLandingGuard } from "./home-landing.guard";

export const homeRoutes: Routes = [
  {
    path: "",
    component: Home,
    // No `data.permission`: authGuard would answer Access Denied before homeLandingGuard could
    // send a role without loads to a page it can use. The landing guard enforces Load.View.
    canActivate: [authGuard, homeLandingGuard],
    data: { breadcrumb: "Home" },
  },
];
