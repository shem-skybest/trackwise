import type { SelectOption } from "../models/select-option";

/**
 * Every Philippine province (plus Metro Manila, which has none) and the region it belongs to. Region
 * names are the short everyday forms already stored on addresses ("NCR", "CALABARZON"), not the
 * PSGC long forms ("Region IV-A (CALABARZON)").
 */
const PH_PROVINCE_REGIONS: Record<string, string> = {
  "Metro Manila": "NCR",
  Abra: "CAR",
  Apayao: "CAR",
  Benguet: "CAR",
  Ifugao: "CAR",
  Kalinga: "CAR",
  "Mountain Province": "CAR",
  "Ilocos Norte": "Ilocos Region",
  "Ilocos Sur": "Ilocos Region",
  "La Union": "Ilocos Region",
  Pangasinan: "Ilocos Region",
  Batanes: "Cagayan Valley",
  Cagayan: "Cagayan Valley",
  Isabela: "Cagayan Valley",
  "Nueva Vizcaya": "Cagayan Valley",
  Quirino: "Cagayan Valley",
  Aurora: "Central Luzon",
  Bataan: "Central Luzon",
  Bulacan: "Central Luzon",
  "Nueva Ecija": "Central Luzon",
  Pampanga: "Central Luzon",
  Tarlac: "Central Luzon",
  Zambales: "Central Luzon",
  Batangas: "CALABARZON",
  Cavite: "CALABARZON",
  Laguna: "CALABARZON",
  Quezon: "CALABARZON",
  Rizal: "CALABARZON",
  Marinduque: "MIMAROPA",
  "Occidental Mindoro": "MIMAROPA",
  "Oriental Mindoro": "MIMAROPA",
  Palawan: "MIMAROPA",
  Romblon: "MIMAROPA",
  Albay: "Bicol Region",
  "Camarines Norte": "Bicol Region",
  "Camarines Sur": "Bicol Region",
  Catanduanes: "Bicol Region",
  Masbate: "Bicol Region",
  Sorsogon: "Bicol Region",
  Aklan: "Western Visayas",
  Antique: "Western Visayas",
  Capiz: "Western Visayas",
  Guimaras: "Western Visayas",
  Iloilo: "Western Visayas",
  "Negros Occidental": "Negros Island Region",
  "Negros Oriental": "Negros Island Region",
  Siquijor: "Negros Island Region",
  Bohol: "Central Visayas",
  Cebu: "Central Visayas",
  Biliran: "Eastern Visayas",
  "Eastern Samar": "Eastern Visayas",
  Leyte: "Eastern Visayas",
  "Northern Samar": "Eastern Visayas",
  Samar: "Eastern Visayas",
  "Southern Leyte": "Eastern Visayas",
  "Zamboanga del Norte": "Zamboanga Peninsula",
  "Zamboanga del Sur": "Zamboanga Peninsula",
  "Zamboanga Sibugay": "Zamboanga Peninsula",
  Sulu: "Zamboanga Peninsula",
  Bukidnon: "Northern Mindanao",
  Camiguin: "Northern Mindanao",
  "Lanao del Norte": "Northern Mindanao",
  "Misamis Occidental": "Northern Mindanao",
  "Misamis Oriental": "Northern Mindanao",
  "Davao de Oro": "Davao Region",
  "Davao del Norte": "Davao Region",
  "Davao del Sur": "Davao Region",
  "Davao Occidental": "Davao Region",
  "Davao Oriental": "Davao Region",
  Cotabato: "SOCCSKSARGEN",
  Sarangani: "SOCCSKSARGEN",
  "South Cotabato": "SOCCSKSARGEN",
  "Sultan Kudarat": "SOCCSKSARGEN",
  "Agusan del Norte": "Caraga",
  "Agusan del Sur": "Caraga",
  "Dinagat Islands": "Caraga",
  "Surigao del Norte": "Caraga",
  "Surigao del Sur": "Caraga",
  Basilan: "BARMM",
  "Lanao del Sur": "BARMM",
  "Maguindanao del Norte": "BARMM",
  "Maguindanao del Sur": "BARMM",
  "Tawi-Tawi": "BARMM",
};

/** Old or PSGC spellings that name one of the provinces above, keyed in lower case. */
const PH_PROVINCE_ALIASES: Record<string, string> = {
  ncr: "Metro Manila",
  "national capital region": "Metro Manila",
  "samar (western samar)": "Samar",
  "western samar": "Samar",
  "compostela valley": "Davao de Oro",
  "cotabato (north cotabato)": "Cotabato",
  "north cotabato": "Cotabato",
};

const PROVINCE_BY_LOWER_NAME = new Map(
  Object.keys(PH_PROVINCE_REGIONS).map((name) => [name.toLowerCase(), name]),
);

export const PH_PROVINCES_OPTIONS: SelectOption[] = Object.keys(PH_PROVINCE_REGIONS)
  .sort((a, b) => a.localeCompare(b))
  .map((name) => ({ label: name, value: name }));

/**
 * The canonical spelling of a Philippine province, or null when the name isn't one. Accepts any
 * casing, the aliases above, and PSGC's Metro Manila districts ("Ncr, Second District").
 */
export function canonicalPhProvince(name: string | null | undefined): string | null {
  const key = name?.trim().toLowerCase();
  if (!key) {
    return null;
  }
  if (/^ncr\b/.test(key)) {
    return "Metro Manila";
  }
  return PROVINCE_BY_LOWER_NAME.get(key) ?? PH_PROVINCE_ALIASES[key] ?? null;
}

/** The region a Philippine province belongs to, or null when the province isn't recognised. */
export function phRegionOfProvince(province: string | null | undefined): string | null {
  const canonical = canonicalPhProvince(province);
  return canonical ? PH_PROVINCE_REGIONS[canonical] : null;
}

/**
 * PSGC and Mapbox name provinces their own way ("Ncr, Second District", "Samar (Western Samar)");
 * this stores the everyday spelling and the region it belongs to, falling back to the source's
 * own names for anything the province list doesn't recognise.
 */
export function phProvinceAndRegion(
  sourceProvince: string | null | undefined,
  sourceRegion: string | null | undefined,
): { province: string; region: string } {
  const province = canonicalPhProvince(sourceProvince) ?? sourceProvince ?? "";
  return { province, region: phRegionOfProvince(province) ?? sourceRegion ?? "" };
}
