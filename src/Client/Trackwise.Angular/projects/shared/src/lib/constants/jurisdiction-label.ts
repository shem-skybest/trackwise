import { CA_PROVINCES_OPTIONS } from "./ca-provinces.options";
import { US_STATES_OPTIONS } from "./us-states.options";

/**
 * IFTA / tax jurisdictions are stored as an ISO country code plus a region code
 * (e.g. `US` + `CA`). These helpers turn that pair into human-readable text so tables
 * and exports show "California", not "US-CA". Regions we don't have a name for fall
 * back to the raw code, which keeps unknown jurisdictions legible rather than blank.
 */
const REGION_NAME_BY_COUNTRY: Record<string, Record<string, string>> = {
  US: Object.fromEntries(US_STATES_OPTIONS.map((o) => [o.value, o.label])),
  CA: Object.fromEntries(CA_PROVINCES_OPTIONS.map((o) => [o.value, o.label])),
};

/** `US-CA` style code from a country + region pair. */
export function jurisdictionCode(countryCode?: string | null, region?: string | null): string {
  return `${(countryCode ?? "").toUpperCase()}-${(region ?? "").toUpperCase()}`;
}

/** Region name alone (e.g. "California"), or the raw region code when unknown. */
export function jurisdictionName(countryCode?: string | null, region?: string | null): string {
  const country = (countryCode ?? "").toUpperCase();
  const reg = (region ?? "").toUpperCase();
  return REGION_NAME_BY_COUNTRY[country]?.[reg] ?? reg;
}

/** Name with the code in parentheses (e.g. "California (US-CA)"); code alone when unknown. */
export function jurisdictionLabel(countryCode?: string | null, region?: string | null): string {
  const reg = (region ?? "").toUpperCase();
  const name = jurisdictionName(countryCode, region);
  const code = jurisdictionCode(countryCode, region);
  return name && name !== reg ? `${name} (${code})` : code;
}
