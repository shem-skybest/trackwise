import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  hidden,
  maxLength,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { Api, getBranches, type CommodityDto } from "@trackwise/shared/api";
import {
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  UiToggleField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface CommodityFormValue {
  code: string | null;
  name: string;
  hsCode: string | null;
  description: string | null;
  isHazardous: boolean;
  unNumber: string | null;
  requiresTemperatureControl: boolean;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface CommodityFormModel {
  code: string;
  name: string;
  hsCode: string;
  description: string;
  isHazardous: boolean;
  unNumber: string;
  requiresTemperatureControl: boolean;
  notes: string;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: CommodityFormModel = {
  code: "",
  name: "",
  hsCode: "",
  description: "",
  isHazardous: false,
  unNumber: "",
  requiresTemperatureControl: false,
  notes: "",
  branchId: null,
  isActive: true,
};

/** Mirrors Commodity.NormalizeDigits: HS and UN numbers are stored as digits. */
const digits = (value: string): string => value.replace(/\D/g, "");

@Component({
  selector: "app-commodity-form",
  templateUrl: "./commodity-form.html",
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
    UiSelectField,
    UiTextareaField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class CommodityForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly commodity = input<CommodityDto | null>(null);
  public readonly submitLabel = input("Create Commodity");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it survives
   * a merge, so the edit form shows it read-only rather than offering a rename that would strand
   * every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<CommodityFormValue>();

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a commodity several offices carry is scoped
   * to none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<CommodityFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      maxLength(p.description, 1000, {
        message: "Description must be 1000 characters or fewer.",
      });
      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });

      // Checked on the digits rather than the raw value, matching the server: customs paperwork
      // writes the code with dots, and rejecting "0303.89" would teach stewards to strip them.
      validate(p.hsCode, ({ value }) => {
        const entered = digits(value().trim());
        return value().trim().length === 0 || (entered.length >= 6 && entered.length <= 10)
          ? null
          : { kind: "hsCode", message: "An HS code is between six and ten digits." };
      });

      validate(p.unNumber, ({ value }) => {
        const entered = digits(value().trim());
        return value().trim().length === 0 || entered.length === 4
          ? null
          : { kind: "unNumber", message: "A UN number is four digits." };
      });

      // A UN number is meaningless without the flag, so the field disappears rather than sitting
      // there collecting a value the server would reject. The submit clears it too - hiding a
      // control does not empty it.
      hidden(p.unNumber, { when: ({ valueOf }) => !valueOf(p.isHazardous) });
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
      const commodity = this.commodity();
      if (!commodity) {
        return;
      }

      this.model.set({
        code: commodity.code ?? "",
        name: commodity.name ?? "",
        hsCode: commodity.hsCode ?? "",
        description: commodity.description ?? "",
        isHazardous: commodity.isHazardous ?? false,
        unNumber: commodity.unNumber ?? "",
        requiresTemperatureControl: commodity.requiresTemperatureControl ?? false,
        notes: commodity.notes ?? "",
        branchId: commodity.branchId ?? null,
        isActive: commodity.isActive ?? true,
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
      // A branch list that fails to load must not block creating a commodity; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): CommodityFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Commodity number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),

      // Sent as digits so the form and the importer agree on what was entered.
      hsCode: digits(value.hsCode) || null,
      description: value.description.trim() || null,
      isHazardous: value.isHazardous,

      // Untick hazardous and the number goes with it. The server clears it anyway; doing it here
      // too keeps the request honest about what the steward left on screen.
      unNumber: value.isHazardous ? digits(value.unNumber) || null : null,

      requiresTemperatureControl: value.requiresTemperatureControl,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
    };
  }
}
