import { Component, inject, signal } from "@angular/core";
import { toObservable } from "@angular/core/rxjs-interop";
import { ActivatedRoute, Router } from "@angular/router";
import { filter, switchMap, take } from "rxjs";
import { ErrorState } from "../ui/status/error-state/error-state";
import { Spinner } from "../ui/status/spinner/spinner";
import { AuthService } from "./auth.service";
import { takeReturnUrl } from "./return-url";

const LAST_ATTEMPT_KEY = "trackwise.signInStartedAt";
const LOOP_WINDOW_MS = 15_000;

/**
 * Every portal's sign-in route. It has nothing of its own to show: once the startup auth check
 * settles, a signed-in user goes back to the page a route guard turned them away from, or else to
 * `route.data["homeUrl"]` (default `/home`), and everyone else straight to the IdentityServer's
 * login page.
 *
 * It stops and offers a retry instead of redirecting when the IdentityServer sent back an error,
 * or when this tab started a sign-in moments ago and still came back signed out - a callback that
 * keeps failing would otherwise bounce between the two hosts forever.
 */
@Component({
  selector: "ui-sign-in-redirect",
  templateUrl: "./sign-in-redirect.html",
  imports: [ErrorState, Spinner],
})
export class SignInRedirect {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly failed = signal(false);

  constructor() {
    toObservable(this.authService.authInitialized)
      .pipe(
        filter(Boolean),
        take(1),
        switchMap(() => this.authService.onAuthenticated()),
        take(1),
      )
      .subscribe((isAuthenticated) => {
        if (isAuthenticated) {
          clearLastAttempt();
          this.router.navigateByUrl(
            takeReturnUrl() ?? this.route.snapshot.data["homeUrl"] ?? "/home",
          );
        } else if (this.route.snapshot.queryParamMap.has("error") || startedMomentsAgo()) {
          clearLastAttempt();
          this.failed.set(true);
        } else {
          this.signIn();
        }
      });
  }

  protected signIn(): void {
    this.failed.set(false);
    try {
      sessionStorage.setItem(LAST_ATTEMPT_KEY, Date.now().toString());
    } catch {
      // Storage blocked: sign in anyway, just without the loop guard.
    }
    this.authService.login();
  }
}

function startedMomentsAgo(): boolean {
  try {
    const startedAt = Number(sessionStorage.getItem(LAST_ATTEMPT_KEY));
    return startedAt > 0 && Date.now() - startedAt < LOOP_WINDOW_MS;
  } catch {
    return false;
  }
}

function clearLastAttempt(): void {
  try {
    sessionStorage.removeItem(LAST_ATTEMPT_KEY);
  } catch {
    // Nothing to clear when storage is blocked.
  }
}
