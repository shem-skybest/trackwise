import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  hidden,
  max,
  maxLength,
  min,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import {
  Api,
  getBranches,
  getLocations,
  type Address,
  type CustomerDto,
  type LocationDto,
  type LocationType,
} from "@trackwise/shared/api";
import { locationTypeOptions } from "@trackwise/shared/api/enums";
import {
  AddressForm,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDateField,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { SearchCustomer } from "@/shared/components/search";
import { dateToTimeOfDay, timeOfDayToDate } from "@/shared/utils";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface LocationFormValue {
  code: string | null;
  name: string;
  type: LocationType;
  address: Address | null;
  parentLocationId: string | null;
  latitude: number | null;
  longitude: number | null;
  notes: string | null;
  branchId: string | null;
  customerId: string | null;
  defaultCutoffTime: string | null;
  zoneRadiusMeters: number | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface LocationFormModel {
  code: string;
  name: string;
  type: LocationType;
  address: Address | null;
  parentLocationId: string | null;
  latitude: number | null;
  longitude: number | null;
  notes: string;
  branchId: string | null;
  customer: CustomerDto | null;
  cutoff: Date | null;
  zoneRadiusMeters: number | null;
  isActive: boolean;
}

const ZONE = "zone" as LocationType;

const EMPTY: LocationFormModel = {
  code: "",
  name: "",
  type: "delivery_point" as LocationType,
  address: null,
  parentLocationId: null,
  latitude: null,
  longitude: null,
  notes: "",
  branchId: null,
  customer: null,
  cutoff: null,
  zoneRadiusMeters: null,
  isActive: true,
};

@Component({
  selector: "app-location-form",
  templateUrl: "./location-form.html",
  imports: [
    AddressForm,
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    Stack,
    Surface,
    Typography,
    SearchCustomer,
    UiButton,
    UiDateField,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class LocationForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly location = input<LocationDto | null>(null);
  public readonly submitLabel = input("Create Location");

  /** Pre-selects the owning customer on a new record, e.g. from the customer's own page. */
  public readonly initialCustomer = input<CustomerDto | null>(null);

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it survives
   * a merge, so the edit form shows it read-only rather than offering a rename that would strand
   * every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<LocationFormValue>();

  protected readonly typeOptions = locationTypeOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate automatically.",
  );

  protected readonly isZone = computed(() => this.model().type === ZONE);

  /**
   * "Every branch" is a real choice, not an empty one: a location several offices deliver to is
   * scoped to none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  /** Only zones, because only a zone can contain another location - the server refuses the rest. */
  protected readonly zoneOptions = signal<{ label: string; value: string | null }[]>([
    { label: "No zone", value: null },
  ]);

  protected readonly model = signal<LocationFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      required(p.type, { message: "Type is required." });
      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });

      // A zone is a grouping rather than a place, so it has no address of its own - giving it one
      // of its members' addresses is how a zone quietly becomes a duplicate delivery point.
      required(p.address, {
        when: ({ valueOf }) => valueOf(p.type) !== ZONE,
        message: "Address is required.",
      });

      hidden(p.address, { when: ({ valueOf }) => valueOf(p.type) === ZONE });

      // Coordinates travel together. One half of a pair points at the Gulf of Guinea, which is
      // where every half-filled coordinate on a map ends up.
      validate(p.latitude, ({ valueOf }) =>
        valueOf(p.longitude) !== null && valueOf(p.latitude) === null
          ? { kind: "coordinatePair", message: "Give both latitude and longitude, or neither." }
          : null,
      );

      validate(p.longitude, ({ valueOf }) =>
        valueOf(p.latitude) !== null && valueOf(p.longitude) === null
          ? { kind: "coordinatePair", message: "Give both latitude and longitude, or neither." }
          : null,
      );

      min(p.zoneRadiusMeters, 10, { message: "Under 10 m is inside GPS error." });
      max(p.zoneRadiusMeters, 5000, { message: "Over 5 km is a district, not a site." });
      hidden(p.customer, { when: ({ valueOf }) => valueOf(p.type) === ZONE });
      hidden(p.cutoff, { when: ({ valueOf }) => valueOf(p.type) === ZONE });
      hidden(p.zoneRadiusMeters, { when: ({ valueOf }) => valueOf(p.type) === ZONE });

      min(p.latitude, -90, { message: "Latitude is between -90 and 90." });
      max(p.latitude, 90, { message: "Latitude is between -90 and 90." });
      min(p.longitude, -180, { message: "Longitude is between -180 and 180." });
      max(p.longitude, 180, { message: "Longitude is between -180 and 180." });
    },
    {
      submission: {
        action: async () => {
          this.save.emit(this.toFormValue());
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.loadBranches();
    void this.loadZones();

    effect(() => {
      const customer = this.initialCustomer();
      if (customer && !this.location()) {
        this.model.update((v) => ({ ...v, customer }));
      }
    });

    effect(() => {
      const location = this.location();
      if (!location) {
        return;
      }

      this.model.set({
        code: location.code ?? "",
        name: location.name ?? "",
        type: location.type ?? ("delivery_point" as LocationType),
        address: location.address ?? null,
        parentLocationId: location.parentLocationId ?? null,
        latitude: location.latitude ?? null,
        longitude: location.longitude ?? null,
        notes: location.notes ?? "",
        branchId: location.branchId ?? null,
        // The search control only needs id and name to show the current owner.
        customer: location.customerId
          ? ({ id: location.customerId, name: location.customerName ?? "" } as CustomerDto)
          : null,
        cutoff: timeOfDayToDate(location.defaultCutoffTime),
        zoneRadiusMeters: location.zoneRadiusMeters ?? null,
        isActive: location.isActive ?? true,
      });
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
      // A branch list that fails to load must not block creating a location; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private async loadZones(): Promise<void> {
    try {
      const response = await this.api.invoke(getLocations, {
        Type: ZONE,
        IsActive: true,
        PageSize: 500,
      });

      this.zoneOptions.set([
        { label: "No zone", value: null },

        // The record being edited is filtered out: a location cannot sit inside itself, and the
        // deeper loops the server walks are not worth offering only to refuse.
        ...(response.items ?? [])
          .filter((zone) => zone.id !== this.location()?.id)
          .map((zone) => ({
            label: `${zone.code} - ${zone.name}`,
            value: zone.id ?? null,
          })),
      ]);
    } catch {
      // Same reasoning as the branches: "No zone" is a legitimate answer on its own.
    }
  }

  private toFormValue(): LocationFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Location number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      type: value.type,

      // Dropped for a zone, matching the server. Hiding a control does not empty it, so switching
      // the type after filling the address would otherwise submit one the server then refuses.
      address: value.type === ZONE ? null : value.address,

      parentLocationId: value.parentLocationId,
      latitude: value.latitude,
      longitude: value.longitude,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      customerId: value.type === ZONE ? null : (value.customer?.id ?? null),
      defaultCutoffTime: value.type === ZONE ? null : dateToTimeOfDay(value.cutoff),
      zoneRadiusMeters: value.type === ZONE ? null : value.zoneRadiusMeters,
      isActive: value.isActive,
    };
  }
}
