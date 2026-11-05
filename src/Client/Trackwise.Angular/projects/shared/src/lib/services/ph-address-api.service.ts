import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { from, map, Observable, of } from "rxjs";
import type { Address } from "../api/generated/models";

/**
 * Raw row shapes from the bundled JSON files.
 */
export interface PhRegionRow {
  id: number;
  psgc_code: string;
  region_name: string;
  region_code: string;
}

export interface PhProvinceRow {
  province_code: string;
  province_name: string;
  psgc_code: string;
  region_code: string;
}

export interface PhCityRow {
  city_code: string;
  city_name: string;
  province_code: string;
  psgc_code: string;
  /** Note: the source file calls this `region_desc`; we normalise to `region_code` for callers. */
  region_code: string;
}

export interface PhBarangayRow {
  brgy_code: string;
  brgy_name: string;
  city_code: string;
  province_code: string;
  region_code: string;
}

/**
 * Unified search result shape surfaced by `search()`.
 * The `geographicLevel` field drives how AddressAutocomplete renders the item
 * and which Address fields are filled in.
 */
export interface PhAddressSearchResult {
  code: string;
  name: string;
  geographicLevel: "region" | "province" | "city_municipality" | "barangay";
  /** Parent province name — populated for cities/municipalities and barangays. */
  "sub Province"?: string;
  /** Parent region name — populated for provinces, cities/municipalities and barangays. */
  "sub Region"?: string;
  regionCode?: string;
  provinceCode?: string;
  cityMunicipalityCode?: string;
}

interface PhLocationCache {
  regions: PhRegionRow[];
  provinces: PhProvinceRow[];
  cities: PhCityRow[];
  barangays: PhBarangayRow[];
}

const ASSET_BASE = "assets/ph-address";

/**
 * Bundled-JSON Philippine address service.
 *
 * Fetches all four static JSON files once on first use and keeps them in memory.
 * The `search()` method queries across all four levels (region → province →
 * city/municipality → barangay) and returns ranked results with a normalised
 * `PhAddressSearchResult` shape that `AddressAutocomplete` already understands.
 *
 * Individual getter methods (`getRegions`, `getProvinces`, …) return synchronously
 * once the cache is warm, and the `LocationCascade` component uses them to
 * drive its Region → Province → City → Barangay dropdown cascade.
 */
export class PhAddressApiService {
  private readonly http = inject(HttpClient);

  private cache: PhLocationCache | null = null;
  private cachePromise: Promise<PhLocationCache> | null = null;

  /**
   * Search across all four levels. Returns at most 10 results prioritised by level
   * (regions first, then provinces, cities, barangays).
   */
  search(query: string): Observable<PhAddressSearchResult[]> {
    return this.getCache().pipe(
      map((cache) => {
        const q = query.toLowerCase().trim();
        if (!q) return [];

        const hits: PhAddressSearchResult[] = [];

        // Regions (exact-ish match via includes)
        cache.regions
          .filter((r) => r.region_name.toLowerCase().includes(q))
          .slice(0, 3)
          .forEach((r) =>
            hits.push({
              code: r.region_code,
              name: r.region_name,
              geographicLevel: "region",
            }),
          );

        // Provinces
        cache.provinces
          .filter((p) => p.province_name.toLowerCase().includes(q))
          .slice(0, 5)
          .forEach((p) => {
            const region = cache.regions.find((r) => r.region_code === p.region_code);
            hits.push({
              code: p.province_code,
              name: p.province_name,
              geographicLevel: "province",
              "sub Region": region?.region_name,
              regionCode: p.region_code,
            });
          });

        // Cities / municipalities
        cache.cities
          .filter((c) => c.city_name.toLowerCase().includes(q))
          .slice(0, 5)
          .forEach((c) => {
            const province = cache.provinces.find((p) => p.province_code === c.province_code);
            const region = cache.regions.find((r) => r.region_code === c.region_code);
            hits.push({
              code: c.city_code,
              name: c.city_name,
              geographicLevel: "city_municipality",
              "sub Province": province?.province_name,
              "sub Region": region?.region_name,
              regionCode: c.region_code,
              provinceCode: c.province_code,
            });
          });

        // Barangays
        cache.barangays
          .filter((b) => b.brgy_name.toLowerCase().includes(q))
          .slice(0, 5)
          .forEach((b) => {
            const province = cache.provinces.find((p) => p.province_code === b.province_code);
            const region = cache.regions.find((r) => r.region_code === b.region_code);
            hits.push({
              code: b.brgy_code,
              name: b.brgy_name,
              geographicLevel: "barangay",
              "sub Province": province?.province_name,
              "sub Region": region?.region_name,
              regionCode: b.region_code,
              provinceCode: b.province_code,
              cityMunicipalityCode: b.city_code,
            });
          });

        return hits.slice(0, 10);
      }),
    );
  }

  /** All regions. */
  getRegions(): Observable<PhRegionRow[]> {
    return this.getCache().pipe(map((c) => c.regions));
  }

  /** Provinces, optionally filtered by region code. */
  getProvinces(regionCode?: string): Observable<PhProvinceRow[]> {
    return this.getCache().pipe(
      map((c) =>
        regionCode ? c.provinces.filter((p) => p.region_code === regionCode) : c.provinces,
      ),
    );
  }

  /** Cities / municipalities, optionally filtered by province code. */
  getCities(provinceCode?: string): Observable<PhCityRow[]> {
    return this.getCache().pipe(
      map((c) =>
        provinceCode ? c.cities.filter((ci) => ci.province_code === provinceCode) : c.cities,
      ),
    );
  }

  /** Barangays, optionally filtered by city code. */
  getBarangays(cityCode?: string): Observable<PhBarangayRow[]> {
    return this.getCache().pipe(
      map((c) => (cityCode ? c.barangays.filter((b) => b.city_code === cityCode) : c.barangays)),
    );
  }

  /**
   * Build an `Address` object from a `PhAddressSearchResult`.
   * PSGC has no coordinates, so `center` is left for the caller to resolve.
   */
  resultToAddress(result: PhAddressSearchResult): Address {
    const addr: Address = {
      streetAddress: "",
      cityOrMunicipality: "",
      province: "",
      region: "",
      zipcode: "",
      country: "PH",
    };

    switch (result.geographicLevel) {
      case "region":
        addr.region = result.name;
        break;
      case "province":
        addr.region = result["sub Region"] ?? "";
        addr.province = result.name;
        break;
      case "city_municipality":
        addr.region = result["sub Region"] ?? "";
        addr.province = result["sub Province"] ?? "";
        addr.cityOrMunicipality = result.name;
        break;
      case "barangay":
        addr.region = result["sub Region"] ?? "";
        addr.province = result["sub Province"] ?? "";
        addr.cityOrMunicipality = "";
        break;
    }

    return addr;
  }

  /**
   * Full hierarchy for a given barangay code — used when a barangay is selected so
   * the full Address (barangay + city + province + region) can be assembled.
   */
  getBarangayHierarchy(barangayCode: string): Observable<{
    region: PhRegionRow | null;
    province: PhProvinceRow | null;
    city: PhCityRow | null;
    barangay: PhBarangayRow | null;
  }> {
    return this.getCache().pipe(
      map((c) => ({
        region:
          c.regions.find((r) =>
            c.barangays.some(
              (b) => b.brgy_code === barangayCode && b.region_code === r.region_code,
            ),
          ) ?? null,
        province:
          c.provinces.find((p) =>
            c.barangays.some(
              (b) => b.brgy_code === barangayCode && b.province_code === p.province_code,
            ),
          ) ?? null,
        city:
          c.cities.find((ci) =>
            c.barangays.some((b) => b.brgy_code === barangayCode && b.city_code === ci.city_code),
          ) ?? null,
        barangay: c.barangays.find((b) => b.brgy_code === barangayCode) ?? null,
      })),
    );
  }

  private getCache(): Observable<PhLocationCache> {
    if (this.cache) return of(this.cache);

    if (!this.cachePromise) {
      this.cachePromise = this.fetchAll();
    }

    return from(this.cachePromise);
  }

  private async fetchAll(): Promise<PhLocationCache> {
    const [regions, provinces, citiesRaw, barangays] = await Promise.all([
      this.fetchJson<PhRegionRow[]>(`${ASSET_BASE}/region.json`),
      this.fetchJson<PhProvinceRow[]>(`${ASSET_BASE}/province.json`),
      // The source file uses `region_desc` for the city→region link; normalise it to
      // `region_code` so the rest of the service can use a single column name.
      this.fetchJson<(Omit<PhCityRow, "region_code"> & { region_desc: string })[]>(
        `${ASSET_BASE}/city-source.json`,
      ),
      this.fetchJson<PhBarangayRow[]>(`${ASSET_BASE}/barangay.json`),
    ]);

    const cities: PhCityRow[] = citiesRaw.map((c) => ({
      city_code: c.city_code,
      city_name: c.city_name,
      province_code: c.province_code,
      psgc_code: c.psgc_code,
      region_code: c.region_desc,
    }));

    this.cache = { regions, provinces, cities, barangays };
    return this.cache;
  }

  private fetchJson<T>(url: string): Promise<T> {
    return this.http
      .get<T>(url)
      .toPromise()
      .then((data) => data ?? []) as Promise<T>;
  }
}
