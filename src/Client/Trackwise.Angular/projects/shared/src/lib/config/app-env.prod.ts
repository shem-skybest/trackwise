/** Production replacement for `app-env.ts` (see `fileReplacements` in angular.json); never import directly. */
export const environment = {
  production: true,
  apiUrl: "https://api.skybest.com.ph",
  identityServerUrl: "https://id.skybest.com.ph",
  // Substituted in the built bundle at container start by deploy/docker-entrypoint-spa.sh.
  mapboxToken: "${MAPBOX_TOKEN}",
};
