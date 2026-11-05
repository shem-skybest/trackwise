import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { COUNTRIES_OPTIONS } from "@trackwise/shared";
import { Api, getBranches, type VesselDto, type VesselType } from "@trackwise/shared/api";
import { vesselTypeOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface VesselFormValue {
  code: string | null;
  name: string;
  imoNumber: string | null;
  callSign: string | null;
  flagCountryCode: string | null;
  type: VesselType;
  teuCapacity: number | null;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface VesselFormModel {
  code: string;
  name: string;
  imoNumber: string;
  callSign: string;
  flagCountryCode: string | null;
  type: VesselType;
  teuCapacity: number | null;
  notes: string;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: VesselFormModel = {
  code: "",
  name: "",
  imoNumber: "",
  callSign: "",
  flagCountryCode: null,
  type: "container_ship" as VesselType,
  teuCapacity: null,
  notes: "",
  branchId: null,
  isActive: true,
};

/** Mirrors Vessel.NormalizeImoNumber: the stored value is digits only. */
const imoDigits = (value: string): string => value.replace(/\D/g, "");

@Component({
  selector: "app-vessel-form",
  templateUrl: "./vessel-form.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class VesselForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly vessel = input<VesselDto | null>(null);
  public readonly submitLabel = input("Create Vessel");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it survives
   * a merge, so the edit form shows it read-only rather than offering a rename that would strand
   * every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<VesselFormValue>();

  protected readonly typeOptions = vesselTypeOptions;

  /** Flags are not region-filtered the way a tenant's own addresses are - ships sail under anything. */
  protected readonly flagOptions = [
    { label: "No flag recorded", value: null },
    ...COUNTRIES_OPTIONS,
  ];

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a vessel several offices book is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<VesselFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      required(p.type, { message: "Type is required." });
      maxLength(p.callSign, 20, { message: "Call sign must be 20 characters or fewer." });
      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });

      // Checked on the digits rather than the raw value, matching the server: a steward pasting
      // "IMO 9395044" off a manifest is entering a valid number.
      validate(p.imoNumber, ({ value }) => {
        const raw = value().trim();
        return raw.length === 0 || imoDigits(raw).length === 7
          ? null
          : { kind: "imoNumber", message: "An IMO number is seven digits." };
      });

      // Zero would read as "this ship carries no containers", which an empty field already says
      // properly for a tanker.
      min(p.teuCapacity, 1, { message: "Leave TEU capacity empty rather than zero." });
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

    effect(() => {
      const vessel = this.vessel();
      if (!vessel) {
        return;
      }

      this.model.set({
        code: vessel.code ?? "",
        name: vessel.name ?? "",
        imoNumber: vessel.imoNumber ?? "",
        callSign: vessel.callSign ?? "",
        flagCountryCode: vessel.flagCountryCode ?? null,
        type: vessel.type ?? ("container_ship" as VesselType),
        teuCapacity: vessel.teuCapacity ?? null,
        notes: vessel.notes ?? "",
        branchId: vessel.branchId ?? null,
        isActive: vessel.isActive ?? true,
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
      // A branch list that fails to load must not block creating a vessel; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): VesselFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Vessel number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),

      // Sent as digits so the form and the importer agree on what was entered.
      imoNumber: imoDigits(value.imoNumber) || null,

      callSign: value.callSign.trim().toUpperCase() || null,
      flagCountryCode: value.flagCountryCode,
      type: value.type,
      teuCapacity: value.teuCapacity,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
    };
  }
}
