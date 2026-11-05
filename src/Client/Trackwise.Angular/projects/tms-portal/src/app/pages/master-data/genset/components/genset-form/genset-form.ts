import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  readonly as readonlyWhen,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { Api, getBranches, type GensetDto, type GensetStatus } from "@trackwise/shared/api";
import { gensetStatusOptions } from "@trackwise/shared/api/enums";
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
export interface GensetFormValue {
  code: string | null;
  name: string;
  status: GensetStatus;
  serialNumber: string | null;
  make: string | null;
  model: string | null;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface GensetFormModel {
  code: string;
  name: string;
  status: GensetStatus;
  serialNumber: string;
  make: string;
  model: string;
  notes: string;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: GensetFormModel = {
  code: "",
  name: "",
  status: "available" as GensetStatus,
  serialNumber: "",
  make: "",
  model: "",
  notes: "",
  branchId: null,
  isActive: true,
};

@Component({
  selector: "app-genset-form",
  templateUrl: "./genset-form.html",
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
export class GensetForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly genset = input<GensetDto | null>(null);
  public readonly submitLabel = input("Create Genset");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it
   * survives a merge, so the edit form shows it read-only rather than offering a rename that would
   * strand every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<GensetFormValue>();

  protected readonly statusOptions = gensetStatusOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate one automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a genset several offices share is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<GensetFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      required(p.status, { message: "Status is required." });

      maxLength(p.serialNumber, 60, { message: "Serial number must be 60 characters or fewer." });
      maxLength(p.make, 100, { message: "Make must be 100 characters or fewer." });
      maxLength(p.model, 100, { message: "Model must be 100 characters or fewer." });
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
      const genset = this.genset();
      if (!genset) {
        return;
      }

      this.model.set({
        code: genset.code ?? "",
        name: genset.name ?? "",
        status: genset.status ?? ("available" as GensetStatus),
        serialNumber: genset.serialNumber ?? "",
        make: genset.make ?? "",
        model: genset.model ?? "",
        notes: genset.notes ?? "",
        branchId: genset.branchId ?? null,
        isActive: genset.isActive ?? true,
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
      // A branch list that fails to load must not block creating a genset; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): GensetFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Genset number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      status: value.status,
      serialNumber: value.serialNumber.trim().toUpperCase() || null,
      make: value.make.trim() || null,
      model: value.model.trim() || null,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
    };
  }
}
