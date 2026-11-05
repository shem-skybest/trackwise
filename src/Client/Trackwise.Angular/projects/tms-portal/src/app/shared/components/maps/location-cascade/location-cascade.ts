import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
} from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import type { SelectOption } from "@trackwise/shared";
import {
  PhAddressApiService,
  type PhBarangayRow,
  type PhCityRow,
  type PhProvinceRow,
  type PhRegionRow,
} from "@trackwise/shared/services";
import { Grid, UiFormField, UiSelectField } from "@trackwise/shared/ui";

/** Selection emitted by the cascade once a complete region > province > city/municipality (optionally barangay) chain is picked. */
export interface LocationSelection {
  region: PhRegionRow | null;
  province: PhProvinceRow | null;
  cityMunicipality: PhCityRow | null;
  barangay: PhBarangayRow | null;
}

/**
 * Cascading location picker for Philippine addresses, backed by the bundled JSON in
 * `public/assets/ph-address/`. Renders Region > Province > City/Municipality > Barangay
 * dropdowns. Selection changes flow through `selectionChange` for map-pin integration.
 */
@Component({
  selector: "app-location-cascade",
  templateUrl: "./location-cascade.html",
  imports: [Grid, UiFormField, UiSelectField],
})
export class LocationCascade {
  private readonly phAddressApi = inject(PhAddressApiService);
  private readonly destroyRef = inject(DestroyRef);

  /** Disabled state for all dropdowns. */
  public readonly disabled = input<boolean>(false);

  /** Emitted whenever the user picks a complete location. */
  public readonly selectionChange = output<LocationSelection>();

  protected readonly selectedRegionCode = signal<string | null>(null);
  protected readonly selectedProvinceCode = signal<string | null>(null);
  protected readonly selectedCityCode = signal<string | null>(null);
  protected readonly selectedBarangayCode = signal<string | null>(null);

  protected readonly regions = toSignal(this.phAddressApi.getRegions(), {
    initialValue: [] as PhRegionRow[],
  });

  protected readonly provinces = signal<PhProvinceRow[]>([]);
  protected readonly citiesMunicipalities = signal<PhCityRow[]>([]);
  protected readonly barangays = signal<PhBarangayRow[]>([]);

  protected readonly regionOptions = computed<SelectOption[]>(() =>
    this.regions().map((r) => ({ label: r.region_name, value: r.region_code })),
  );
  protected readonly provinceOptions = computed<SelectOption[]>(() =>
    this.provinces().map((p) => ({ label: p.province_name, value: p.province_code })),
  );
  protected readonly cityOptions = computed<SelectOption[]>(() =>
    this.citiesMunicipalities().map((c) => ({ label: c.city_name, value: c.city_code })),
  );
  protected readonly barangayOptions = computed<SelectOption[]>(() =>
    this.barangays().map((b) => ({ label: b.brgy_name, value: b.brgy_code })),
  );

  constructor() {
    effect(() => {
      const regionCode = this.selectedRegionCode();
      this.phAddressApi
        .getProvinces(regionCode ?? undefined)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((list) => this.provinces.set(list));
    });

    effect(() => {
      const provinceCode = this.selectedProvinceCode();
      this.phAddressApi
        .getCities(provinceCode ?? undefined)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((list) => this.citiesMunicipalities.set(list));
    });

    effect(() => {
      const cityCode = this.selectedCityCode();
      this.phAddressApi
        .getBarangays(cityCode ?? undefined)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((list) => this.barangays.set(list));
    });

    effect(() => {
      const regionCode = this.selectedRegionCode();
      const provinceCode = this.selectedProvinceCode();
      const cityCode = this.selectedCityCode();
      const barangayCode = this.selectedBarangayCode();

      if (!regionCode || !provinceCode || !cityCode) {
        return;
      }

      const region = this.regions().find((r) => r.region_code === regionCode) ?? null;
      const province = this.provinces().find((p) => p.province_code === provinceCode) ?? null;
      const cityMunicipality =
        this.citiesMunicipalities().find((c) => c.city_code === cityCode) ?? null;
      const barangay = barangayCode
        ? (this.barangays().find((b) => b.brgy_code === barangayCode) ?? null)
        : null;

      this.selectionChange.emit({ region, province, cityMunicipality, barangay });
    });
  }

  protected onRegionChange(code: string | null): void {
    this.selectedRegionCode.set(code);
    this.selectedProvinceCode.set(null);
    this.selectedCityCode.set(null);
    this.selectedBarangayCode.set(null);
    this.provinces.set([]);
    this.citiesMunicipalities.set([]);
    this.barangays.set([]);
  }

  protected onProvinceChange(code: string | null): void {
    this.selectedProvinceCode.set(code);
    this.selectedCityCode.set(null);
    this.selectedBarangayCode.set(null);
    this.citiesMunicipalities.set([]);
    this.barangays.set([]);
  }

  protected onCityChange(code: string | null): void {
    this.selectedCityCode.set(code);
    this.selectedBarangayCode.set(null);
    this.barangays.set([]);
  }

  protected onBarangayChange(code: string | null): void {
    this.selectedBarangayCode.set(code);
  }
}
