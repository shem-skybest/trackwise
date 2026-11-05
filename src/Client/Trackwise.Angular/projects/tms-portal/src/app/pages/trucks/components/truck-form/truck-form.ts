import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  max,
  min,
  readonly as readonlyWhen,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { DRIVING_ROLES } from "@trackwise/shared";
import {
  Api,
  getBranches,
  getEmployees,
  type AdrEquipmentDto,
  type EmployeeDto,
  type HazmatClass,
  type TruckDto,
  type TruckOwnership,
  type TruckStatus,
  type TruckTonnageClass,
  type TruckType,
} from "@trackwise/shared/api";
import {
  truckOwnershipOptions,
  truckStatusOptions,
  truckTonnageClassOptions,
  truckTypeOptions,
} from "@trackwise/shared/api/enums";
import {
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiAutocompleteField,
  UiButton,
  UiDateField,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { dateToIso, isoToDate } from "@/shared/utils";
import { TruckFormTips } from "./truck-form-tips";
import { TruckHazmatSection } from "./truck-hazmat-section";
import { TruckVinField } from "./truck-vin-field";

/** Payload emitted to the parent, which owns the save request. */
export interface TruckFormData {
  truckNumber: string;
  /** Display name, e.g. "ABF3727 (4T Reefer)". Null on create lets the API derive one. */
  name: string | null;
  truckType: TruckType;
  truckStatus: TruckStatus;
  mainDriver: EmployeeDto | null;
  secondaryDriver: EmployeeDto | null;
  vehicleCapacity: number | null;
  /** Payload-weight class (2T/4T/10T) - null for body types with no tonnage concept. */
  tonnageClass: TruckTonnageClass | null;
  /** Gross vehicle weight from the CR, in kg - decides the Philippine DL code a driver needs. */
  grossVehicleWeightKg: number | null;
  make: string | null;
  model: string | null;
  year: number | null;
  vin: string | null;
  licensePlate: string | null;
  licensePlateState: string | null;
  /** Vehicle-register fields: the company's asset tag, the pre-plate LTO sticker, CR engine number. */
  assetCode: string | null;
  conductionSticker: string | null;
  engineNumber: string | null;
  /** Purchase date as an API `DateOnly` ("2025-03-15"). */
  acquiredOn: string | null;
  supplier: string | null;
  /** Null when nobody has confirmed whether the unit is owned, leased or subcontracted. */
  ownership: TruckOwnership | null;
  adrEquipment: AdrEquipmentDto;
  isHazmatPlacarded: boolean;
  /** Master-data fields. Owning branch, or null when every branch uses this truck. */
  branchId: string | null;
  /** Whether the truck is still part of the fleet. Edit-only - always true on create. */
  isActive: boolean;
}

/**
 * Flat model behind the form. The ADR fields live alongside the rest and are nested only on emit.
 *
 * `make`, `model`, `vin`, `licensePlate`, `licensePlateState`, and `orangePlateNumber` are optional
 * on the wire (`TruckFormData`/DTO) but bind to `ui-text-field`, whose `FormValueControl<string>`
 * is invariant - so they're `string` here (empty string = "no value"), coerced to `null` only when
 * building the outgoing `TruckFormData`.
 */
export interface TruckFormModel {
  truckNumber: string;
  name: string;
  truckType: TruckType;
  truckStatus: TruckStatus;
  mainDriver: EmployeeDto | null;
  secondaryDriver: EmployeeDto | null;
  vehicleCapacity: number | null;
  tonnageClass: TruckTonnageClass | null;
  grossVehicleWeightKg: number | null;
  make: string;
  model: string;
  year: number | null;
  vin: string;
  licensePlate: string;
  licensePlateState: string;
  assetCode: string;
  conductionSticker: string;
  engineNumber: string;
  acquiredOn: Date | null;
  supplier: string;
  ownership: TruckOwnership | null;
  isAdrCertified: boolean;
  adrCertExpiresAt: Date | null;
  adrAllowedClasses: HazmatClass[];
  orangePlateNumber: string;
  isHazmatPlacarded: boolean;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: TruckFormModel = {
  truckNumber: "",
  name: "",
  truckType: "freight_truck",
  truckStatus: "available",
  mainDriver: null,
  secondaryDriver: null,
  vehicleCapacity: null,
  tonnageClass: null,
  grossVehicleWeightKg: null,
  make: "",
  model: "",
  year: null,
  vin: "",
  licensePlate: "",
  licensePlateState: "",
  assetCode: "",
  conductionSticker: "",
  engineNumber: "",
  acquiredOn: null,
  supplier: "",
  ownership: null,
  isAdrCertified: false,
  adrCertExpiresAt: null,
  adrAllowedClasses: [],
  orangePlateNumber: "",
  isHazmatPlacarded: false,
  branchId: null,
  isActive: true,
};

@Component({
  selector: "app-truck-form",
  templateUrl: "./truck-form.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    Stack,
    Surface,
    TruckFormTips,
    TruckHazmatSection,
    TruckVinField,
    Typography,
    UiAutocompleteField,
    UiButton,
    UiDateField,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class TruckForm {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);

  public readonly mode = input.required<"create" | "edit">();
  public readonly initial = input<TruckDto | null>(null);
  public readonly isLoading = input(false);

  public readonly save = output<TruckFormData>();
  public readonly remove = output<void>();

  protected readonly truckTypes = truckTypeOptions;
  protected readonly truckStatuses = truckStatusOptions;
  protected readonly tonnageClasses = truckTonnageClassOptions;
  /** "Not confirmed" is a real answer: a unit on the dispatch boards whose papers nobody has checked yet. */
  protected readonly ownershipOptions: { label: string; value: TruckOwnership | null }[] = [
    { label: "Not confirmed", value: null },
    ...truckOwnershipOptions,
  ];
  protected readonly suggestedDrivers = signal<EmployeeDto[]>([]);

  /**
   * "Every branch" is a real choice, not an empty one: a truck several offices share is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<TruckFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.truckNumber, { message: "Truck number is required." });
      required(p.name, {
        message: "Name is required.",
        when: () => this.mode() === "edit",
      });
      required(p.truckType, { message: "Truck type is required." });
      required(p.truckStatus, { message: "Status is required." });
      min(p.vehicleCapacity, 1, { message: "Vehicle capacity must be at least 1." });
      max(p.vehicleCapacity, 12, { message: "Vehicle capacity cannot exceed 12." });
      min(p.grossVehicleWeightKg, 500, {
        message: "Gross vehicle weight must be at least 500 kg.",
      });
      max(p.grossVehicleWeightKg, 60_000, {
        message: "Gross vehicle weight cannot exceed 60,000 kg.",
      });
      min(p.year, 1900, { message: "Year must be 1900 or later." });
      max(p.year, 2100, { message: "Year cannot be later than 2100." });

      // Immutable after creation: it's the handle other systems join on and it survives a merge,
      // so renaming it would strand every legacy key pointing at this record.
      readonlyWhen(p.truckNumber, { when: () => this.mode() === "edit" });
    },
    {
      submission: {
        action: async () => {
          const value = this.model();
          this.save.emit({
            truckNumber: value.truckNumber,
            name: value.name.trim() || null,
            truckType: value.truckType,
            truckStatus: value.truckStatus,
            mainDriver: value.mainDriver,
            secondaryDriver: value.secondaryDriver,
            vehicleCapacity: value.vehicleCapacity,
            tonnageClass: value.tonnageClass,
            grossVehicleWeightKg: value.grossVehicleWeightKg,
            // Optional text fields hold "" in the model (ui-text-field is FormValueControl<string>);
            // the wire (TruckFormData) is string | null, same as the old reactive form's `?? null`.
            make: value.make || null,
            model: value.model || null,
            year: value.year,
            vin: value.vin || null,
            licensePlate: value.licensePlate || null,
            licensePlateState: value.licensePlateState || null,
            assetCode: value.assetCode || null,
            conductionSticker: value.conductionSticker || null,
            engineNumber: value.engineNumber || null,
            acquiredOn: dateToIso(value.acquiredOn),
            supplier: value.supplier || null,
            ownership: value.ownership,
            adrEquipment: {
              isAdrCertified: value.isAdrCertified,
              adrCertExpiresAt: value.adrCertExpiresAt?.toISOString() ?? null,
              allowedClasses: value.adrAllowedClasses,
              orangePlateNumber: value.orangePlateNumber || null,
            },
            isHazmatPlacarded: value.isHazmatPlacarded,
            branchId: value.branchId,
            isActive: value.isActive,
          });
          return undefined;
        },
      },
    },
  );

  protected readonly isCarHauler = computed(() => this.model().truckType === "car_hauler");

  constructor() {
    void this.loadBranches();

    // Hydrate the model from the async-loaded DTO whenever it arrives.
    effect(() => {
      const initial = this.initial();
      if (initial) {
        this.hydrate(initial);
      }
    });

    // Vehicle capacity only applies to car haulers; clear it for any other type, mirroring the
    // old `truckType.valueChanges` subscription.
    effect(() => {
      if (this.model().truckType !== "car_hauler" && this.model().vehicleCapacity !== null) {
        this.model.update((v) => ({ ...v, vehicleCapacity: null }));
      }
    });
  }

  private async loadBranches(): Promise<void> {
    try {
      const response = await this.api.invoke(getBranches, { IsActive: true, PageSize: 500 });

      this.branchOptions.set([
        { label: "Every branch", value: null },
        ...(response.items ?? []).map((branch) => ({
          label: `${branch.code} - ${branch.name}`,
          value: branch.id ?? null,
        })),
      ]);
    } catch {
      // A branch list that fails to load must not block creating a truck; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  protected async searchDriver(event: { query: string }): Promise<void> {
    const result = await this.api.invoke(getEmployees, {
      Search: event.query,
      Roles: [...DRIVING_ROLES],
    });
    if (result.items) {
      this.suggestedDrivers.set(result.items);
    }
  }

  protected askRemove(): void {
    this.toastService.confirm({
      message: "Are you sure you want to delete this truck?",
      accept: () => this.remove.emit(),
    });
  }

  private hydrate(initial: TruckDto): void {
    const adr = initial.adrEquipment;
    this.model.set({
      truckNumber: initial.number ?? "",
      name: initial.name ?? "",
      truckType: initial.type ?? "freight_truck",
      truckStatus: initial.status ?? "available",
      mainDriver: initial.mainDriver ?? null,
      secondaryDriver: initial.secondaryDriver ?? null,
      vehicleCapacity: initial.vehicleCapacity ?? null,
      tonnageClass: initial.tonnageClass ?? null,
      grossVehicleWeightKg: initial.grossVehicleWeightKg ?? null,
      make: initial.make ?? "",
      model: initial.model ?? "",
      year: initial.year ?? null,
      vin: initial.vin ?? "",
      licensePlate: initial.licensePlate ?? "",
      licensePlateState: initial.licensePlateState ?? "",
      assetCode: initial.assetCode ?? "",
      conductionSticker: initial.conductionSticker ?? "",
      engineNumber: initial.engineNumber ?? "",
      acquiredOn: isoToDate(initial.acquiredOn),
      supplier: initial.supplier ?? "",
      ownership: initial.ownership ?? null,
      isAdrCertified: adr?.isAdrCertified ?? false,
      adrCertExpiresAt: adr?.adrCertExpiresAt ? new Date(adr.adrCertExpiresAt) : null,
      adrAllowedClasses: adr?.allowedClasses ?? [],
      orangePlateNumber: adr?.orangePlateNumber ?? "",
      isHazmatPlacarded: initial.isHazmatPlacarded ?? false,
      branchId: initial.branchId ?? null,
      isActive: initial.isActive ?? true,
    });
  }
}
