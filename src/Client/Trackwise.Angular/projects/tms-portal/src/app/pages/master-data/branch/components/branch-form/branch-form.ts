import { Component, computed, effect, input, output, signal } from "@angular/core";
import {
  email as emailValidator,
  form,
  FormField,
  FormRoot,
  maxLength,
  readonly as readonlyWhen,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import type { Address, BranchDto } from "@trackwise/shared/api";
import {
  AddressForm,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiTextField,
  UiToggleField,
} from "@trackwise/shared/ui";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface BranchFormValue {
  code: string | null;
  name: string;
  shortName: string | null;
  address: Address | null;
  phone: string | null;
  email: string | null;
  taxId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface BranchFormModel {
  code: string;
  name: string;
  shortName: string;
  address: Address | null;
  phone: string;
  email: string;
  taxId: string;
  isActive: boolean;
}

const EMPTY: BranchFormModel = {
  code: "",
  name: "",
  shortName: "",
  address: null,
  phone: "",
  email: "",
  taxId: "",
  isActive: true,
};

@Component({
  selector: "app-branch-form",
  templateUrl: "./branch-form.html",
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
    UiTextField,
    UiToggleField,
  ],
})
export class BranchForm {
  public readonly isLoading = input(false);
  public readonly branch = input<BranchDto | null>(null);
  public readonly submitLabel = input("Create Branch");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it
   * survives a merge, so the edit form shows it read-only rather than offering a rename that
   * would strand every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<BranchFormValue>();

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "Leave blank to allocate one automatically.",
  );

  protected readonly model = signal<BranchFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });
      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });
      maxLength(p.shortName, 20, { message: "Short name must be 20 characters or fewer." });
      emailValidator(p.email, { message: "Enter a valid email address." });
      maxLength(p.taxId, 50, { message: "TIN must be 50 characters or fewer." });
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
      const branch = this.branch();
      if (!branch) {
        return;
      }

      this.model.set({
        code: branch.code ?? "",
        name: branch.name ?? "",
        shortName: branch.shortName ?? "",
        address: branch.address ?? null,
        phone: branch.phone ?? "",
        email: branch.email ?? "",
        taxId: branch.taxId ?? "",
        isActive: branch.isActive ?? true,
      });
    });
  }

  private toFormValue(): BranchFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Branch number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      shortName: value.shortName.trim() || null,
      address: value.address,
      phone: value.phone.trim() || null,
      email: value.email.trim() || null,
      taxId: value.taxId.trim() || null,
      isActive: value.isActive,
    };
  }
}
