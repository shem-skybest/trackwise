import { HttpClient } from "@angular/common/http";
import {
  booleanAttribute,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { phProvinceAndRegion } from "@trackwise/shared";
import type { Address, GeoPoint } from "@trackwise/shared/api";
import type {
  PhBarangayRow,
  PhCityRow,
  PhProvinceRow,
  PhRegionRow,
} from "@trackwise/shared/services";
import {
  Alert,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiTextField,
} from "@trackwise/shared/ui";
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  firstValueFrom,
  of,
  Subject,
  switchMap,
} from "rxjs";
import { TenantService } from "@/core/services/tenant.service";
import { environment } from "@/env";
import { AddressPickerMap } from "../address-picker-map/address-picker-map";
import { LocationCascade, type LocationSelection } from "../location-cascade/location-cascade";

/** Forward geocoding query assembled from the dialog's street + cascade fields. */
interface GeocodeQuery {
  street: string;
  barangay: string | null;
  city: string | null;
  province: string | null;
  region: string | null;
}

interface GeocodeAttempt {
  q: string;
  types?: string;
}

/** Result from Mapbox v6 forward geocoding. Coordinates can live in either
 *  `geometry.coordinates` (legacy) or `properties.coordinates` (v6 Search Box). */
interface GeocodeFeature {
  id: string;
  properties: {
    feature_type?: string;
    coordinates?: {
      longitude: number;
      latitude: number;
    };
    context?: {
      address?: { name: string };
      place?: { name: string };
      region?: { name: string };
      postcode?: { name: string };
      country?: { country_code: string };
    };
    name?: string;
  };
  geometry?: {
    coordinates?: [number, number]; // [lng, lat]
  };
}

/** Emit shape when the user confirms the address selection. */
interface AddressSelection {
  address: Address;
  location: GeoPoint;
}

@Component({
  selector: "app-address-picker-dialog",
  templateUrl: "./address-picker-dialog.html",
  imports: [
    Alert,
    AddressPickerMap,
    LocationCascade,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiTextField,
  ],
})
export class AddressPickerDialog {
  /** Two-way bindable dialog visibility. */
  public readonly open = model<boolean>(false);

  /** Emitted when the dialog becomes visible. */
  public readonly opened = output<void>();

  /** Emitted when the dialog becomes hidden. */
  public readonly closed = output<void>();

  /** Dialog header title. */
  public readonly header = input<string>("Select Address");

  /** Dialog width. */
  public readonly width = input<string>("900px");

  /** Dialog max-width. */
  public readonly maxWidth = input<string>("95vw");

  /** Whether the dialog is closable (X button + Escape). */
  public readonly closable = input(true, { transform: booleanAttribute });

  /** Whether the dialog is modal (blocks backdrop clicks). */
  public readonly modal = input(true, { transform: booleanAttribute });

  /** Whether the dialog is draggable. */
  public readonly draggable = input(true, { transform: booleanAttribute });

  /** Default country for the cascade (PH for now; future: dynamic). */
  public readonly defaultCountry = input<string>("PH");

  /** Emitted when the user confirms the selection. */
  public readonly confirm = output<AddressSelection>();

  /** Emitted when the user cancels/closes. */
  public readonly cancel = output<void>();

  /** Forward geocoding debounce subject. */
  private readonly geocodeSubject = new Subject<GeocodeQuery>();

  /** Pin positions to look up a ZIP code for. */
  private readonly zipLookupSubject = new Subject<GeoPoint>();

  /** A looked-up ZIP never overwrites one the user typed. */
  private zipTypedByUser = false;

  /** State from the location cascade. */
  protected readonly selectedRegion = signal<PhRegionRow | null>(null);
  protected readonly selectedProvince = signal<PhProvinceRow | null>(null);
  protected readonly selectedCity = signal<PhCityRow | null>(null);
  protected readonly selectedBarangay = signal<PhBarangayRow | null>(null);

  /** Text inputs. */
  protected readonly streetAddress = signal<string>("");
  protected readonly subdivision = signal<string>("");
  protected readonly zipcode = signal<string>("");

  /** Map marker and center. */
  protected readonly marker = signal<GeoPoint | null>(null);
  protected readonly mapCenter = signal<GeoPoint>({ longitude: 121.0, latitude: 14.0 });

  /** Forward geocoding loading state. */
  protected readonly isGeocoding = signal(false);

  /** Whether geocoding was attempted but returned no results. */
  protected readonly geocodeNotFound = signal(false);

  /** Map wrapper reference, captured via `viewChild` for `flyTo()` calls. */
  private readonly mapRef = viewChild<AddressPickerMap>(AddressPickerMap);

  private readonly http = inject(HttpClient);
  private readonly tenantService = inject(TenantService);

  constructor() {
    // Reset all state when the dialog opens from the parent's [(open)] binding
    effect(() => {
      if (this.open()) {
        this.resetState();
      }
    });

    // Forward geocoding driver: debounced user-typed street address
    this.geocodeSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged((a, b) => this.queryEquals(a, b)),
        switchMap((query) => this.doGeocode(query)),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (result) => {
          this.isGeocoding.set(false);
          if (result) {
            this.mapCenter.set(result);
            this.mapRef()?.flyTo(result);
            this.geocodeNotFound.set(false);
          } else {
            // No match — invite the user to drop the pin manually
            this.geocodeNotFound.set(true);
          }
        },
        error: () => {
          this.isGeocoding.set(false);
          this.geocodeNotFound.set(true);
        },
      });

    this.zipLookupSubject
      .pipe(
        debounceTime(200),
        switchMap((point) => this.lookUpZipcode(point)),
        takeUntilDestroyed(),
      )
      .subscribe((zip) => {
        if (zip && !this.zipTypedByUser) {
          this.zipcode.set(zip);
        }
      });
  }

  /** Reset all form state to defaults. Called on every reopen. */
  private resetState(): void {
    this.streetAddress.set("");
    this.subdivision.set("");
    this.zipcode.set("");
    this.zipTypedByUser = false;
    this.marker.set(null);
    this.mapCenter.set({ longitude: 121.0, latitude: 14.0 });
    this.isGeocoding.set(false);
    this.geocodeNotFound.set(false);
    // Reset cascade selections
    this.selectedRegion.set(null);
    this.selectedProvince.set(null);
    this.selectedCity.set(null);
    this.selectedBarangay.set(null);
  }

  /** Assemble a Mapbox forward-geocoding query from current dialog state. */
  private assembleQuery(): GeocodeQuery {
    return {
      street: this.streetAddress().trim(),
      barangay: this.selectedBarangay()?.brgy_name ?? null,
      city: this.selectedCity()?.city_name ?? null,
      province: this.selectedProvince()?.province_name ?? null,
      region: this.selectedRegion()?.region_name ?? null,
    };
  }

  private queryEquals(a: GeocodeQuery, b: GeocodeQuery): boolean {
    return (
      a.street === b.street &&
      a.barangay === b.barangay &&
      a.city === b.city &&
      a.province === b.province &&
      a.region === b.region
    );
  }

  /**
   * Tries street, then barangay, then city, and keeps the first result that lies in the chosen
   * city. Mapbox's geocoder doesn't know store names ("Vista Prime Food") and misreads PSGC's
   * names - "Ncr, Fourth District" sent verbatim matched Fourth Street in Malabon - so an
   * unchecked first hit can land in the wrong city entirely.
   */
  private async doGeocode(query: GeocodeQuery): Promise<GeoPoint | null> {
    const city = query.city ? toMapboxCityName(query.city) : null;
    const province = query.province ? toMapboxProvinceName(query.province) : null;
    const area = [city, province].filter((part): part is string => !!part);

    const attempts: GeocodeAttempt[] = [];
    if (query.street) {
      attempts.push({ q: [query.street, query.barangay, ...area].filter(Boolean).join(", ") });
    }
    if (query.barangay) {
      attempts.push({
        q: [query.barangay, ...area].join(", "),
        types: "neighborhood,locality,place",
      });
    }
    if (area.length > 0) {
      attempts.push({
        q: area.join(", "),
        types: city ? "place,locality" : "region,district,place",
      });
    } else if (query.region) {
      attempts.push({ q: toMapboxProvinceName(query.region), types: "region" });
    }

    for (const attempt of attempts) {
      const feature = await this.forwardGeocode(attempt);
      if (feature && (!query.city || isInCity(feature, query.city))) {
        return featureCoordinates(feature);
      }
    }
    return null;
  }

  private async forwardGeocode(attempt: GeocodeAttempt): Promise<GeocodeFeature | null> {
    const lang = this.tenantService.tenantData()?.settings?.language ?? "en";
    const params: Record<string, string> = {
      q: attempt.q,
      access_token: environment.mapboxToken,
      language: lang,
      country: this.defaultCountry(),
      // Limit results to Philippine bounding box
      bbox: "116.0,4.5,129.0,21.0",
      limit: "1",
    };
    if (attempt.types) {
      params["types"] = attempt.types;
    }

    const resp = await firstValueFrom(
      this.http
        .get<{ features: GeocodeFeature[] }>(`https://api.mapbox.com/search/geocode/v6/forward`, {
          params,
        })
        .pipe(catchError(() => of({ features: [] } as { features: GeocodeFeature[] }))),
    );
    return resp.features?.[0] ?? null;
  }

  /** PSGC data has no ZIP codes, so the ZIP comes from Mapbox's postcode area under the pin. */
  private async lookUpZipcode(point: GeoPoint): Promise<string | null> {
    const { longitude, latitude } = point;
    if (longitude == null || latitude == null) {
      return null;
    }

    const resp = await firstValueFrom(
      this.http
        .get<{ features: GeocodeFeature[] }>(`https://api.mapbox.com/search/geocode/v6/reverse`, {
          params: {
            longitude,
            latitude,
            types: "postcode",
            access_token: environment.mapboxToken,
          },
        })
        .pipe(catchError(() => of({ features: [] } as { features: GeocodeFeature[] }))),
    );
    return resp.features?.[0]?.properties?.name ?? null;
  }

  /** Emit the final address + location when the user clicks Confirm. */
  private emitSelection(): void {
    // Map the cascade rows into Address fields
    const regionRow = this.selectedRegion();
    const provinceRow = this.selectedProvince();
    const cityRow = this.selectedCity();
    const barangayRow = this.selectedBarangay();
    const { province, region } = phProvinceAndRegion(
      provinceRow?.province_name,
      regionRow?.region_name,
    );

    const addressObj: Address = {
      streetAddress: this.streetAddress() || "",
      barangay: barangayRow?.brgy_name ?? null,
      cityOrMunicipality: cityRow?.city_name ?? "",
      province,
      region,
      zipcode: this.zipcode() || "",
      country: this.defaultCountry(),
    };

    const location: GeoPoint = this.marker()!;

    this.confirm.emit({ address: addressObj, location });
    this.open.set(false);
  }

  /** Called when the user selects a new region/province/city/barangay from the cascade. */
  protected onLocationSelection(selection: LocationSelection): void {
    this.selectedRegion.set(selection.region ?? null);
    this.selectedProvince.set(selection.province ?? null);
    this.selectedCity.set(selection.cityMunicipality ?? null);
    this.selectedBarangay.set(selection.barangay ?? null);
    this.geocodeNotFound.set(false);

    this.isGeocoding.set(true);
    this.geocodeSubject.next(this.assembleQuery());
  }

  /** Called when the user types in the Street Address field. */
  protected onStreetChange(value: string): void {
    this.streetAddress.set(value ?? "");
    this.geocodeNotFound.set(false);

    // Debounced forward geocoding
    const query = this.assembleQuery();
    this.isGeocoding.set(true);
    this.geocodeSubject.next(query);
  }

  /** Called when the user types in the Subdivision field. */
  protected onSubdivisionChange(value: string): void {
    this.subdivision.set(value ?? "");
  }

  /** Called when the user types in the Zipcode field. Clearing it lets the pin fill it again. */
  protected onZipcodeChange(value: string): void {
    const zip = value ?? "";
    if (zip === this.zipcode()) {
      return;
    }
    this.zipcode.set(zip);
    this.zipTypedByUser = zip.trim().length > 0;
  }

  /** Called when the user clicks on empty map space. */
  protected onMapClick(point: GeoPoint): void {
    this.marker.set(point);
    this.mapCenter.set(point);
    this.geocodeNotFound.set(false);
    this.zipLookupSubject.next(point);
  }

  /**
   * Drops a pin at the current map center when forward geocoding returned no match.
   * The user has already panned the map to the right area; this commits that center
   * as the chosen location.
   */
  protected useCurrentMapCenter(): void {
    this.marker.set(this.mapCenter());
    this.geocodeNotFound.set(false);
    this.zipLookupSubject.next(this.mapCenter());
  }

  /** Called when the user drags/re-clicks the existing marker. */
  protected onMarkerDrag(point: GeoPoint): void {
    this.marker.set(point);
    this.mapCenter.set(point);
    this.zipLookupSubject.next(point);
  }

  /** Called when the user clicks Cancel. */
  protected onCancel(): void {
    this.cancel.emit();
    this.open.set(false);
  }

  /** Called when the user clicks Confirm. */
  protected onConfirm(): void {
    this.emitSelection();
  }

  /** Whether the Confirm button should be enabled. */
  protected readonly isValid = computed(() => {
    const hasStreet = this.streetAddress().trim().length > 0;
    const hasZip = this.zipcode().trim().length > 0;
    const hasMarker = this.marker() !== null;
    return hasStreet && hasZip && hasMarker;
  });
}

/** PSGC's "City Of Makati" is "Makati City" to Mapbox. */
function toMapboxCityName(psgcCity: string): string {
  const cityOf = /^city of (.+)$/i.exec(psgcCity.trim());
  return cityOf ? `${cityOf[1]} City` : psgcCity.trim();
}

/** PSGC splits Metro Manila into "Ncr, ... District" pseudo-provinces that Mapbox doesn't know. */
function toMapboxProvinceName(psgcProvince: string): string {
  return /^(ncr\b|national capital region)/i.test(psgcProvince.trim())
    ? "Metro Manila"
    : psgcProvince.trim();
}

/** "City Of Parañaque" and "Paranaque City" both become "paranaque". */
function normalizePlaceName(name: string): string {
  return name
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .split(/[^a-z]+/)
    .filter((word) => word && word !== "city" && word !== "of")
    .join(" ");
}

function isInCity(feature: GeocodeFeature, psgcCity: string): boolean {
  const props = feature.properties;
  const place =
    props.context?.place?.name ?? (props.feature_type === "place" ? props.name : undefined);
  return !!place && normalizePlaceName(place) === normalizePlaceName(psgcCity);
}

/** Mapbox v6 puts coordinates in `properties.coordinates`; fall back to `geometry.coordinates`. */
function featureCoordinates(feature: GeocodeFeature): GeoPoint | null {
  const coords = feature.properties.coordinates;
  if (coords) {
    return { longitude: coords.longitude, latitude: coords.latitude };
  }
  const geometry = feature.geometry?.coordinates;
  return geometry && geometry.length >= 2
    ? { longitude: geometry[0], latitude: geometry[1] }
    : null;
}
