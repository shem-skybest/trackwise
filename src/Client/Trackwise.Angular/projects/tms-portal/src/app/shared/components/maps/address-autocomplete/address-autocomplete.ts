import { TitleCasePipe } from "@angular/common";
import { HttpClient, HttpParams } from "@angular/common/http";
import {
  Component,
  computed,
  ElementRef,
  inject,
  input,
  linkedSignal,
  model,
  output,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import type { FormValueControl } from "@angular/forms/signals";
import { focusFirstControl, phProvinceAndRegion } from "@trackwise/shared";
import type { Address, GeoPoint } from "@trackwise/shared/api";
import { PhAddressApiService, type PhAddressSearchResult } from "@trackwise/shared/services";
import { UiTextField } from "@trackwise/shared/ui";
import { regionAllowedCountries } from "@trackwise/shared/utils";
import { catchError, debounceTime, distinctUntilChanged, of, Subject, switchMap } from "rxjs";
import { TenantService } from "@/core/services";
import { environment } from "@/env";
import type {
  MapboxGeocodingFeature,
  MapboxGeocodingResponse,
  GeoPoint as MapboxGeoPoint,
} from "@/shared/types/mapbox";
import { Converters } from "@/shared/utils";
import { SKYBEST_PH_BRANCHES, SKYBEST_TENANT_NAME, type KnownBranch } from "./skybest-branches";

const SEARCH_DEBOUNCE_MS = 250;

// Mapbox rejects more than 5 ISO codes in a single `country` filter.
const MAPBOX_MAX_COUNTRY_CODES = 5;

@Component({
  selector: "app-address-autocomplete",
  templateUrl: "./address-autocomplete.html",
  styleUrl: "./address-autocomplete.css",
  imports: [UiTextField, TitleCasePipe],
})
export class AddressAutocomplete implements FormValueControl<Address | null> {
  private readonly http = inject(HttpClient);
  private readonly tenantService = inject(TenantService);
  private readonly phAddressApi = inject(PhAddressApiService);
  private isTouched = false;
  private readonly accessToken = environment.mapboxToken;
  protected readonly searchResults = signal<MapboxGeocodingFeature[]>([]);
  protected readonly phSearchResults = signal<PhAddressSearchResult[]>([]);
  /** True once a query has actually come back from Mapbox with zero matches - distinct from "no search yet". */
  protected readonly noMatches = signal(false);
  private readonly querySubject = new Subject<string>();
  /** Drives whether the branch quick-picks may show at all - only while the field has focus. */
  private readonly hasFocus = signal(false);

  /**
   * Skybest's own branch offices don't geocode via Mapbox in the Philippines (see
   * `handleClickAddress`'s doc comment), so they're offered as a fixed quick-pick list instead of
   * relying on search. Gated to Skybest's tenant slug (not id - a demo tenant's id is a fresh
   * random guid on every reseed, but its slug is stable) so no other PH tenant sees them.
   */
  protected readonly matchingBranches = computed<readonly KnownBranch[]>(() => {
    if (!this.hasFocus() || this.tenantService.tenantData()?.name !== SKYBEST_TENANT_NAME) {
      return [];
    }

    const query = (this.addressString() ?? "").trim().toLowerCase();
    if (!query) {
      return SKYBEST_PH_BRANCHES;
    }

    // Bidirectional: covers both a short partial ("manila" ⊂ "Manila Branch") and someone
    // typing/pasting the branch's real full address, which contains the city/label as a
    // substring rather than the other way round.
    return SKYBEST_PH_BRANCHES.filter((b) => {
      const label = b.label.toLowerCase();
      const city = b.address.cityOrMunicipality?.toLowerCase() ?? "";
      return label.includes(query) || query.includes(label) || (!!city && query.includes(city));
    });
  });

  /** The control's value. Required by `FormValueControl`. */
  public readonly value = model<Address | null>(null);
  /** Driven by the Reactive Forms bridge; gates all user interaction. */
  public readonly disabled = input<boolean>(false);
  /** Raised the first time the field is interacted with so the form marks it touched. */
  public readonly touch = output<void>();

  /**
   * Text rendered in the input. Reseeds from `value` whenever the form pushes a new
   * address (the old `writeValue` behaviour); the user's typing overrides it until the
   * next `value` change. `addressString` never writes back to `value`, so there is no
   * value -> parse -> value loop.
   */
  protected readonly addressString = linkedSignal<string | null>(() =>
    Converters.addressToString(this.value()),
  );

  public readonly field = input("");
  public readonly placeholder = input("Type address...");
  // When omitted, falls back to the tenant's region (see `effectiveCountry`).
  public readonly country = input<string | null>(null);
  public readonly forceSelection = input(false);

  private readonly effectiveCountry = computed<string>(() => {
    const override = this.country();
    if (override) {
      return override;
    }

    const region = this.tenantService.tenantData()?.settings?.region;
    return regionAllowedCountries(region)
      .slice(0, MAPBOX_MAX_COUNTRY_CODES)
      .map((c) => c.toLowerCase())
      .join(",");
  });

  private readonly effectiveLanguage = computed<string>(
    () => this.tenantService.tenantData()?.settings?.language ?? "en",
  );

  /**
   * Whether the current search should use the Philippine Address API.
   * True when the country filter is PH or when the tenant's region is PH.
   */
  private readonly usePhApi = computed(() => {
    const country = this.country();
    const isPhTenant = this.tenantService.tenantData()?.settings?.region === "ph";
    return country === "PH" || country === null || country === undefined || isPhTenant;
  });

  public readonly selectedAddress = output<SelectedAddressEvent>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    this.querySubject
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((query) => {
          // Use Philippine Address API for PH locations
          if (this.usePhApi()) {
            return this.phAddressApi
              .search(query)
              .pipe(catchError(() => of<PhAddressSearchResult[]>([])));
          }

          // Fall back to Mapbox for other countries
          let params = new HttpParams()
            .set("q", query)
            .set("access_token", this.accessToken)
            .set("types", "address")
            .set("language", this.effectiveLanguage());

          const countryFilter = this.effectiveCountry();
          if (countryFilter) {
            params = params.set("country", countryFilter);
          }

          return this.http
            .get<MapboxGeocodingResponse>("https://api.mapbox.com/search/geocode/v6/forward", {
              params,
            })
            .pipe(
              catchError(() =>
                of<MapboxGeocodingResponse>({
                  attribution: "",
                  type: "FeatureCollection",
                  features: [],
                }),
              ),
            );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((response) => {
        if (this.usePhApi() && Array.isArray(response)) {
          const results = response as PhAddressSearchResult[];
          this.phSearchResults.set(results);
          this.searchResults.set([]);
          this.noMatches.set(results.length === 0);
        } else {
          const mapboxResponse = response as MapboxGeocodingResponse;
          const features = mapboxResponse.features || [];
          this.searchResults.set(features);
          this.phSearchResults.set([]);
          this.noMatches.set(features.length === 0);
        }
      });
  }

  /** Signal Forms calls this via `FieldState.focusBoundControl()`. */
  public focus(options?: FocusOptions): void {
    focusFirstControl(this.host.nativeElement, options);
  }

  protected handleAddressInputChange(query: string): void {
    if (this.disabled()) {
      return;
    }

    // `ui-text-field` is a `FormValueControl<string>`, so it hands us a plain string. Mirror it
    // into `addressString` (the linkedSignal that renders the input) before searching.
    this.addressString.set(query);
    this.noMatches.set(false);

    if (!query) {
      this.markAsTouched();
      this.value.set(null);
      this.searchResults.set([]);
      this.phSearchResults.set([]);
      return;
    }

    this.querySubject.next(query);
  }

  /**
   * Handle selection of a Philippine address result from the bundled JSON API.
   * Maps the geographic level to the appropriate Address fields.
   */
  protected handleClickPhAddress(result: PhAddressSearchResult): void {
    if (this.disabled()) {
      return;
    }

    // For barangays, resolve the full hierarchy to get city/municipality info
    if (result.geographicLevel === "barangay") {
      this.phAddressApi
        .getBarangayHierarchy(result.code)
        .pipe(takeUntilDestroyed())
        .subscribe((hierarchy) => {
          const { province, region } = phProvinceAndRegion(
            hierarchy.province?.province_name,
            hierarchy.region?.region_name,
          );
          const addressObj: Address = {
            streetAddress: "",
            barangay: hierarchy.barangay?.brgy_name ?? null,
            cityOrMunicipality: hierarchy.city?.city_name ?? "",
            province,
            region,
            zipcode: "",
            country: "PH",
          };

          this.value.set(addressObj);
          this.selectedAddress.emit({
            address: addressObj,
            // Bundled JSON has no coordinates; parent must obtain the actual point via map click
            center: null,
          });

          this.phSearchResults.set([]);
          this.noMatches.set(false);
          this.markAsTouched();
        });
      return;
    }

    // Regions, provinces, and cities/municipalities are self-contained in the result.
    const { province, region } =
      result.geographicLevel === "region"
        ? { province: "", region: result.name }
        : phProvinceAndRegion(
            result.geographicLevel === "province" ? result.name : result["sub Province"],
            result["sub Region"],
          );
    const addressObj: Address = {
      streetAddress: "",
      cityOrMunicipality: result.geographicLevel === "city_municipality" ? result.name : "",
      province,
      region,
      zipcode: "",
      country: "PH",
    };

    this.value.set(addressObj);
    this.selectedAddress.emit({
      address: addressObj,
      // Bundled JSON has no coordinates; parent must obtain the actual point via map click
      center: null,
    });

    this.phSearchResults.set([]);
    this.noMatches.set(false);
    this.markAsTouched();
  }

  protected handleClickAddress(geocodingFeature: MapboxGeocodingFeature): void {
    if (this.disabled()) {
      return;
    }

    const context = geocodingFeature.properties.context;
    // `context` only carries ancestors coarser than the selected feature (e.g. a city-level pick
    // has no `address`), so every piece is optional here - `AddressValidator` on the backend is
    // what actually enforces a complete address, and rejects an incomplete one with a clear error.
    const street = context.address?.name ?? geocodingFeature.properties.name;
    const city = context.place?.name;
    const region = context.region?.name;
    const zipCode = context.postcode?.name;
    // Use the ISO-3166-1 alpha-2 code (Mapbox returns it lowercase, e.g. "us"),
    // uppercased to match the rest of the app. The backend requires a 2-letter code.
    const country = context.country?.country_code?.toUpperCase();
    // Mapbox's `region` is the first-level division: a US state, but a province in the Philippines,
    // whose actual region sits one level above and isn't in the context at all.
    const ph = country === "PH" ? phProvinceAndRegion(region, region) : null;

    const addressObj: Address = {
      streetAddress: street ?? "",
      cityOrMunicipality: city ?? "",
      province: ph?.province ?? region ?? "",
      region: ph?.region ?? region ?? "",
      zipcode: zipCode ?? "",
      country: country ?? "",
    };

    // Reseeds `addressString` via the linkedSignal above.
    this.value.set(addressObj);
    this.selectedAddress.emit({
      address: addressObj,
      center: lngLatToGeoPoint(geocodingFeature.geometry.coordinates),
    });

    this.searchResults.set([]);
    this.noMatches.set(false);
    this.markAsTouched();
  }

  protected handleClickBranch(branch: KnownBranch): void {
    if (this.disabled()) {
      return;
    }

    this.value.set(branch.address);
    this.selectedAddress.emit({
      address: branch.address,
      center: lngLatToGeoPoint(branch.center),
    });

    this.searchResults.set([]);
    this.noMatches.set(false);
    this.hasFocus.set(false);
    this.markAsTouched();
  }

  protected handleInputFocus(): void {
    this.hasFocus.set(true);
  }

  protected handleInputFocusOut(): void {
    // Delay the execution to allow click event to be processed (in case an address is clicked from the list)
    setTimeout(() => {
      const hadSelectableOptions =
        this.searchResults().length > 0 ||
        this.phSearchResults().length > 0 ||
        this.matchingBranches().length > 0;
      if (this.forceSelection() && hadSelectableOptions) {
        this.value.set(null);
        this.addressString.set(null);
        this.searchResults.set([]);
        this.phSearchResults.set([]);
      }
      this.noMatches.set(false);
      this.hasFocus.set(false);
    }, 100);
  }

  private markAsTouched(): void {
    if (!this.isTouched) {
      this.isTouched = true;
      this.touch.emit();
    }
  }
}

/** Convert a Mapbox `[lng, lat]` tuple to the API's `{longitude, latitude}` shape. */
function lngLatToGeoPoint([lng, lat]: MapboxGeoPoint): GeoPoint {
  return { longitude: lng, latitude: lat };
}

export interface SelectedAddressEvent {
  address: Address;
  /**
   * The picked coordinate. `null` for sources without embedded coordinates (the bundled PH
   * PSGC data has none) - the parent must obtain the coordinate from a follow-up map click.
   */
  center: GeoPoint | null;
}
