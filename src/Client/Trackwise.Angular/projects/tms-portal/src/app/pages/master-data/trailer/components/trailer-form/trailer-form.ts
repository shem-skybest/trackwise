import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  max,
  maxLength,
  min,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { Api, getBranches, type TrailerDto, type TrailerType } from "@trackwise/shared/api";
import { trailerTypeOptions } from "@trackwise/shared/api/enums";
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
export interface TrailerFormValue {
  code: string | null;
  name: string;
  type: TrailerType;
  vin: string | null;
  licensePlate: string | null;
  licensePlateState: string | null;
  make: string | null;
  model: string | null;
  year: number | null;
  axleCount: number | null;
  maxLoadCapacityKg: number | null;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface TrailerFormModel {
  code: string;
  name: string;
  type: TrailerType;
  vin: string;
  licensePlate: string;
  licensePlateState: string;
  make: string;
  model: string;
  year: number | null;
  axleCount: number | null;
  maxLoadCapacityKg: number | null;
  notes: string;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: TrailerFormModel = {
  code: "",
  name: "",
  type: "dry_van" as TrailerType,
  vin: "",
  licensePlate: "",
  licensePlateState: "",
  make: "",
  model: "",
  year: null,
  axleCount: null,
  maxLoadCapacityKg: null,
  notes: "",
  branchId: null,
  isActive: true,
};

@Component({
  selector: "app-trailer-form",
  templateUrl: "./trailer-form.html",
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
export class TrailerForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly trailer = input<TrailerDto | null>(null);
  public readonly submitLabel = input("Create Trailer");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it
   * survives a merge, so the edit form shows it read-only rather than offering a rename that would
   * strand every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<TrailerFormValue>();

  protected readonly typeOptions = trailerTypeOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate one automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a trailer several offices share is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<TrailerFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      required(p.type, { message: "Type is required." });

      validate(p.vin, ({ value }) => {
        const raw = value().trim();
        return raw.length === 0 || raw.length === 17
          ? null
          : { kind: "vin", message: "A VIN is 17 characters." };
      });

      maxLength(p.licensePlate, 20, { message: "License plate must be 20 characters or fewer." });
      maxLength(p.licensePlateState, 10, { message: "State must be 10 characters or fewer." });
      maxLength(p.make, 100, { message: "Make must be 100 characters or fewer." });
      maxLength(p.model, 100, { message: "Model must be 100 characters or fewer." });

      min(p.year, 1980, { message: "Enter a plausible year." });
      max(p.year, new Date().getFullYear() + 1, { message: "Enter a plausible year." });

      min(p.axleCount, 1, { message: "Leave axle count empty rather than zero." });
      min(p.maxLoadCapacityKg, 1, { message: "Leave load capacity empty rather than zero." });

      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });
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
      const trailer = this.trailer();
      if (!trailer) {
        return;
      }

      this.model.set({
        code: trailer.code ?? "",
        name: trailer.name ?? "",
        type: trailer.type ?? ("dry_van" as TrailerType),
        vin: trailer.vin ?? "",
        licensePlate: trailer.licensePlate ?? "",
        licensePlateState: trailer.licensePlateState ?? "",
        make: trailer.make ?? "",
        model: trailer.model ?? "",
        year: trailer.year ?? null,
        axleCount: trailer.axleCount ?? null,
        maxLoadCapacityKg: trailer.maxLoadCapacityKg ?? null,
        notes: trailer.notes ?? "",
        branchId: trailer.branchId ?? null,
        isActive: trailer.isActive ?? true,
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
      // A branch list that fails to load must not block creating a trailer; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): TrailerFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Trailer number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      type: value.type,
      vin: value.vin.trim().toUpperCase() || null,
      licensePlate: value.licensePlate.trim() || null,
      licensePlateState: value.licensePlateState.trim().toUpperCase() || null,
      make: value.make.trim() || null,
      model: value.model.trim() || null,
      year: value.year,
      axleCount: value.axleCount,
      maxLoadCapacityKg: value.maxLoadCapacityKg,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
    };
  }
}
