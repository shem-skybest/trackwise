const RETURN_URL_KEY = "trackwise.returnUrl";

/**
 * Remembers the page a signed-out user tried to open, so signing in lands them back on it
 * instead of the home page. Kept in sessionStorage: the sign-in round trip to the
 * IdentityServer stays in the same tab, and a stale value can't leak into another tab.
 */
export function rememberReturnUrl(url: string): void {
  if (!isInAppPath(url)) {
    return;
  }
  try {
    sessionStorage.setItem(RETURN_URL_KEY, url);
  } catch {
    // Storage blocked: the user lands on the home page instead.
  }
}

/** Returns the remembered page once, then forgets it. */
export function takeReturnUrl(): string | null {
  try {
    const url = sessionStorage.getItem(RETURN_URL_KEY);
    sessionStorage.removeItem(RETURN_URL_KEY);
    return url && isInAppPath(url) ? url : null;
  } catch {
    return null;
  }
}

/** Only a path inside this app - never `//host` or an absolute URL someone could plant. */
function isInAppPath(url: string): boolean {
  return url.startsWith("/") && !url.startsWith("//") && url !== "/";
}
