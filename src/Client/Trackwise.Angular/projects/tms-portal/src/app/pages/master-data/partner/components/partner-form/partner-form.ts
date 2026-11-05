import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  email as emailValidator,
  form,
  FormField,
  FormRoot,
  maxLength,
  minLength,
  readonly as readonlyWhen,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import {
  Api,
  getBranches,
  type Address,
  type BusinessPartnerDto,
  type BusinessPartnerRole,
} from "@trackwise/shared/api";
import { businessPartnerRoleOptions } from "@trackwise/shared/api/enums";
import {
  AddressForm,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiMultiSelectField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface PartnerFormValue {
  code: string | null;
  name: string;
  roles: BusinessPartnerRole[];
  address: Address | null;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  taxId: string | null;
  branchId: string | null;
  notes: string | null;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface PartnerFormModel {
  code: string;
  name: string;
  roles: BusinessPartnerRole[];
  address: Address | null;
  contactName: string;
  email: string;
  phone: string;
  taxId: string;
  branchId: string | null;
  notes: string;
}

const EMPTY: PartnerFormModel = {
  code: "",
  name: "",
  roles: [],
  address: null,
  contactName: "",
  email: "",
  phone: "",
  taxId: "",
  branchId: null,
  notes: "",
};

/** Mirrors BusinessPartner.NormalizeTaxId: the stored value is digits only. */
const tinDigits = (value: string): string => value.replace(/\D/g, "");

@Component({
  selector: "app-partner-form",
  templateUrl: "./partner-form.html",
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
    UiMultiSelectField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class PartnerForm {
  private readonly api = inject(Api);

  public readonly isLoading = input(false);
  public readonly partner = input<BusinessPartnerDto | null>(null);
  public readonly submitLabel = input("Create Partner");

  /**
   * Code is immutable after creation: the accounting database and nSFS both join on it, so a rename
   * would strand every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<PartnerFormValue>();

  protected readonly roleOptions = businessPartnerRoleOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - accounting and nSFS join on it."
      : "Leave blank to allocate automatically.",
  );

  /**
   * "Every branch" is a real choice, not an empty one: a shipping line several offices deal with is
   * scoped to none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<PartnerFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      minLength(p.roles, 1, { message: "Select at least one role." });
      emailValidator(p.email, { message: "Enter a valid email address." });
      maxLength(p.taxId, 50, { message: "TIN must be 50 characters or fewer." });
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
      const partner = this.partner();
      if (!partner) {
        return;
      }

      this.model.set({
        code: partner.code ?? "",
        name: partner.name ?? "",
        roles: partner.roles ?? [],
        address: partner.address ?? null,
        contactName: partner.contactName ?? "",
        email: partner.email ?? "",
        phone: partner.phone ?? "",
        taxId: partner.taxId ?? "",
        branchId: partner.branchId ?? null,
        notes: partner.notes ?? "",
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
      // A branch list that fails to load must not block creating a partner; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): PartnerFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the BusinessPartner series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      roles: value.roles,
      address: value.address,
      contactName: value.contactName.trim() || null,
      email: value.email.trim() || null,
      phone: value.phone.trim() || null,

      // Sent as digits so the form and the importer reach the duplicate probe by the same route.
      taxId: tinDigits(value.taxId) || null,

      branchId: value.branchId,
      notes: value.notes.trim() || null,
    };
  }
}
