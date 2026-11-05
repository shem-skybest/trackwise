import type { AppOidcOptions } from "@trackwise/shared/auth";
import { environment } from "@/env";

export const authOidcOptions: AppOidcOptions = {
  authority: environment.identityServerUrl,
  clientId: "trackwise.tmsportal",
  scope: "openid profile offline_access roles tenant trackwise.api.tenant",
  postLoginRoute: "/",
  unauthorizedRoute: "/unauthorized",
};
