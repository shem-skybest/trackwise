import type { Address } from "@trackwise/shared/api";
import type { GeoPoint } from "@/shared/types/mapbox";

/**
 * Tenant slug of "Skybest Logistics Corporation" (master DB `tenants.name`). Gates the branch
 * quick-pick list below so it never leaks into another PH tenant's address search. The slug,
 * not `id`, because a demo tenant's id is a fresh random guid on every reseed.
 */
export const SKYBEST_TENANT_NAME = "ph";

export interface KnownBranch {
  label: string;
  address: Address;
  /** [longitude, latitude]. Mapbox has no address-level coverage for these buildings in the
   *  Philippines, so these are geocoded from the nearest resolvable street/locality, not the
   *  exact doorstep - accurate enough for routing and distance, not for turn-by-turn. */
  center: GeoPoint;
}

/**
 * Skybest Logistics' 5 branch offices. Hardcoded rather than modeled as a backend entity because
 * these addresses' floor/unit/building detail doesn't geocode via Mapbox in the Philippines (see
 * `address-autocomplete.ts` for the fallback parsing this forced) - a fixed, pre-verified list
 * sidesteps that instead of fighting the geocoder on every load.
 */
export const SKYBEST_PH_BRANCHES: readonly KnownBranch[] = [
  {
    label: "Manila Branch",
    address: {
      streetAddress: "10th Flr., Sky I Tower, Building #68 Dasmariñas St.",
      subdivision: "Binondo",
      cityOrMunicipality: "Manila",
      province: "Metro Manila",
      region: "NCR",
      zipcode: "1006",
      country: "PH",
    },
    center: [120.976429, 14.597762],
  },
  {
    label: "Cebu (Mandaue) Branch",
    address: {
      streetAddress: "3rd Flr., Carlos Perez Bldg., A.C. Cortez Ave.",
      subdivision: "Ibabao",
      cityOrMunicipality: "Mandaue City",
      province: "Cebu",
      region: "Region VII",
      zipcode: "6014",
      country: "PH",
    },
    center: [123.94273, 10.326905],
  },
  {
    label: "Bacolod Branch",
    address: {
      streetAddress: "Unit 311-312, O Square Building, 2939 Circumferential Highway",
      cityOrMunicipality: "Bacolod City",
      province: "Negros Occidental",
      region: "Region VI",
      zipcode: "6100",
      country: "PH",
    },
    center: [122.970524, 10.670661],
  },
  {
    label: "Cagayan de Oro Branch",
    address: {
      streetAddress: "4th Flr., Gateway Tower 1, LKKS Zone 11",
      subdivision: "Lapasan",
      cityOrMunicipality: "Cagayan de Oro",
      province: "Misamis Oriental",
      region: "Region X",
      zipcode: "9000",
      country: "PH",
    },
    center: [124.664505, 8.482775],
  },
  {
    label: "Davao Branch",
    address: {
      streetAddress: "Unit 2F, GGO Summit Bldg. Corp., JP Laurel Ave.",
      subdivision: "Bajada",
      cityOrMunicipality: "Davao City",
      province: "Davao del Sur",
      region: "Region XI",
      // Not given in the source address - Davao City's standard zip. Verify before relying on it.
      zipcode: "8000",
      country: "PH",
    },
    center: [125.624062, 7.098687],
  },
];
