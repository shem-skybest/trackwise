import { Component, computed, effect, input, output, signal } from "@angular/core";
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
import type { ContainerTypeDto } from "@trackwise/shared/api";
import { containerTypeCategoryOptions } from "@trackwise/shared/api/enums";
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
export interface ContainerTypeFormValue {
  code: string | null;
  name: string;
  isoSizeTypeCode: string | null;
  category: string;
  lengthFeet: number | null;
  isHighCube: boolean;
  teuFactor: number | null;
  notes: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface ContainerTypeFormModel {
  code: string;
  name: string;
  isoSizeTypeCode: string;
  category: string;
  lengthFeet: number | null;
  isHighCube: boolean;
  teuFactor: number | null;
  notes: string;
  isActive: boolean;
}

const EMPTY: ContainerTypeFormModel = {
  code: "",
  name: "",
  isoSizeTypeCode: "",
  category: "general_purpose",
  lengthFeet: null,
  isHighCube: false,
  teuFactor: null,
  notes: "",
  isActive: true,
};

/** Mirrors ContainerType.NormalizeIsoSizeTypeCode: letters and digits, upper-cased. */
const isoCode = (value: string): string => value.replace(/[^a-zA-Z0-9]/g, "").toUpperCase();

/** The lengths a chassis actually carries. Mirrors the server's rule rather than a free number. */
const LENGTH_OPTIONS = [
  { label: "Not applicable", value: null },
  { label: "10 ft", value: 10 },
  { label: "20 ft", value: 20 },
  { label: "40 ft", value: 40 },
  { label: "45 ft", value: 45 },
  { label: "53 ft", value: 53 },
];

@Component({
  selector: "app-container-type-form",
  templateUrl: "./container-type-form.html",
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
export class ContainerTypeForm {
  public readonly isLoading = input(false);
  public readonly containerType = input<ContainerTypeDto | null>(null);
  public readonly submitLabel = input("Create Container Type");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it survives
   * a merge, so the edit form shows it read-only rather than offering a rename that would strand
   * every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<ContainerTypeFormValue>();

  protected readonly categoryOptions = containerTypeCategoryOptions;
  protected readonly lengthOptions = LENGTH_OPTIONS;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "The yard shorthand, e.g. HC40. Leave blank to allocate automatically.",
  );

  protected readonly model = signal<ContainerTypeFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      maxLength(p.notes, 2000, { message: "Notes must be 2000 characters or fewer." });

      // Checked on the normalised value, matching the server: paperwork writes the code as
      // "22 G1", and rejecting that would teach stewards to strip the space by hand.
      validate(p.isoSizeTypeCode, ({ value }) => {
        const entered = isoCode(value().trim());
        return value().trim().length === 0 || entered.length === 4
          ? null
          : {
              kind: "isoSizeTypeCode",
              message: "An ISO 6346 size-type code is four characters, e.g. 22G1.",
            };
      });

      validate(p.teuFactor, ({ value }) => {
        const entered = value();
        return entered === null || (entered >= 0.5 && entered <= 4)
          ? null
          : {
              kind: "teuFactor",
              message: "A TEU factor is between 0.5 and 4 - a 20' is 1, a 40' is 2, a 45' is 2.25.",
            };
      });

      // Both describe a box's dimensions, so they disappear for equipment that has none rather
      // than sitting there collecting values the server would clear. The submit clears them too -
      // hiding a control does not empty it.
      hidden(p.isHighCube, { when: ({ valueOf }) => valueOf(p.lengthFeet) === null });
      hidden(p.teuFactor, { when: ({ valueOf }) => valueOf(p.lengthFeet) === null });
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
    effect(() => {
      const containerType = this.containerType();
      if (!containerType) {
        return;
      }

      this.model.set({
        code: containerType.code ?? "",
        name: containerType.name ?? "",
        isoSizeTypeCode: containerType.isoSizeTypeCode ?? "",
        category: containerType.category ?? "general_purpose",
        lengthFeet: containerType.lengthFeet ?? null,
        isHighCube: containerType.isHighCube ?? false,
        teuFactor: containerType.teuFactor ?? null,
        notes: containerType.notes ?? "",
        isActive: containerType.isActive ?? true,
      });
    });
  }

  private toFormValue(): ContainerTypeFormValue {
    const value = this.model();
    const hasLength = value.lengthFeet !== null;

    return {
      // Blank means "allocate one from the ContainerType number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      isoSizeTypeCode: isoCode(value.isoSizeTypeCode) || null,
      category: value.category,
      lengthFeet: value.lengthFeet,

      // Clear what the length rule hid. The server clears them anyway; doing it here too keeps the
      // request honest about what the steward left on screen.
      isHighCube: hasLength && value.isHighCube,
      teuFactor: hasLength ? value.teuFactor : null,

      notes: value.notes.trim() || null,
      isActive: value.isActive,
    };
  }
}
