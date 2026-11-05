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
import {
  Api,
  getBranches,
  type Address,
  type CrewUnit,
  type EmploymentType,
  type HelperDto,
} from "@trackwise/shared/api";
import { crewUnitOptions, employmentTypeOptions } from "@trackwise/shared/api/enums";
import {
  AddressForm,
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
export interface HelperFormValue {
  code: string | null;
  name: string;
  phoneNumber: string | null;
  address: Address | null;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
  employmentType: EmploymentType | null;
  homeUnit: CrewUnit | null;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface HelperFormModel {
  code: string;
  name: string;
  phoneNumber: string;
  address: Address | null;
  notes: string;
  branchId: string | null;
  isActive: boolean;
  employmentType: EmploymentType | null;
  homeUnit: CrewUnit | null;
}

const EMPTY: HelperFormModel = {
  code: "",
  name: "",
  phoneNumber: "",
  address: null,
  notes: "",
  branchId: null,
  isActive: true,
  employmentType: null,
  homeUnit: null,
};

@Component({
  selector: "app-helper-form",
  templateUrl: "./helper-form.html",
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
    UiButton,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    UiToggleField,
    ValidatedForm,
  ],
})
export class HelperForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly helper = input<HelperDto | null>(null);
  public readonly submitLabel = input("Create Helper");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it
   * survives a merge, so the edit form shows it read-only rather than offering a rename that would
   * strand every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<HelperFormValue>();

  protected readonly employmentTypes = employmentTypeOptions;
  protected readonly crewUnits = crewUnitOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate one automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a helper several offices share is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<HelperFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      maxLength(p.phoneNumber, 50, { message: "Phone number must be 50 characters or fewer." });
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
      const helper = this.helper();
      if (!helper) {
        return;
      }

      this.model.set({
        code: helper.code ?? "",
        name: helper.name ?? "",
        phoneNumber: helper.phoneNumber ?? "",
        address: helper.address ?? null,
        notes: helper.notes ?? "",
        branchId: helper.branchId ?? null,
        isActive: helper.isActive ?? true,
        employmentType: helper.employmentType ?? null,
        homeUnit: helper.homeUnit ?? null,
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
      // A branch list that fails to load must not block creating a helper; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): HelperFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Helper number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      phoneNumber: value.phoneNumber.trim() || null,
      address: value.address,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
      employmentType: value.employmentType,
      homeUnit: value.homeUnit,
    };
  }
}
