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
import { COUNTRIES_OPTIONS, regionAllowedCountries } from "@trackwise/shared";
import {
  Api,
  getBranches,
  type Address,
  type TerminalDto,
  type TerminalType,
} from "@trackwise/shared/api";
import { terminalTypeOptions } from "@trackwise/shared/api/enums";
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
import { TenantService } from "@/core/services/tenant.service";

/** What the parent receives on submit: blanks collapse to null for the API. */
export interface PortFormValue {
  code: string | null;
  name: string;
  countryCode: string;
  type: TerminalType;
  address: Address | null;
  notes: string | null;
  branchId: string | null;
  isActive: boolean;
}

/** What the controls bind to. Text fields hold "" rather than null so the validators typecheck. */
interface PortFormModel {
  code: string;
  name: string;
  countryCode: string;
  type: TerminalType;
  address: Address | null;
  notes: string;
  branchId: string | null;
  isActive: boolean;
}

const EMPTY: PortFormModel = {
  code: "",
  name: "",
  countryCode: "",
  type: "sea_port" as TerminalType,
  address: null,
  notes: "",
  branchId: null,
  isActive: true,
};

@Component({
  selector: "app-port-form",
  templateUrl: "./port-form.html",
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
export class PortForm {
  private readonly api = inject(Api);
  private readonly tenantService = inject(TenantService);

  public readonly isLoading = input(false);
  public readonly port = input<TerminalDto | null>(null);
  public readonly submitLabel = input("Create Port");

  /**
   * Code is immutable after creation: it is the stable handle other systems join on and it survives
   * a merge, so the edit form shows it read-only rather than offering a rename that would strand
   * every legacy key pointing at this record.
   */
  public readonly isEdit = input(false);

  public readonly save = output<PortFormValue>();

  protected readonly typeOptions = terminalTypeOptions;

  protected readonly codeHint = computed(() =>
    this.isEdit()
      ? "Fixed after creation - other systems join on it."
      : "UN/LOCODE where the port has one. Leave blank to allocate automatically.",
  );

  protected readonly tenantRegion = computed(
    () => this.tenantService.tenantData()?.settings?.region ?? "us",
  );

  protected readonly countryOptions = computed(() => {
    const allowed = new Set(regionAllowedCountries(this.tenantRegion()));
    return COUNTRIES_OPTIONS.filter((opt) => allowed.has(opt.value));
  });

  /**
   * "Every branch" is a real choice, not an empty one: a port several offices share is scoped to
   * none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<PortFormModel>({ ...EMPTY });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Name is required." });
      maxLength(p.name, 200, { message: "Name must be 200 characters or fewer." });

      // Deliberately no five-character rule: not every legacy port has a real UN/LOCODE, and
      // requiring one pushes stewards into inventing codes other systems will not recognise.
      maxLength(p.code, 30, { message: "Code must be 30 characters or fewer." });
      readonlyWhen(p.code, { when: () => this.isEdit() });

      required(p.countryCode, { message: "Country is required." });
      required(p.type, { message: "Type is required." });
      required(p.address, { message: "Address is required." });
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
      const port = this.port();
      if (!port) {
        return;
      }

      this.model.set({
        code: port.code ?? "",
        name: port.name ?? "",
        countryCode: port.countryCode ?? "",
        type: port.type ?? ("sea_port" as TerminalType),
        address: port.address ?? null,
        notes: port.notes ?? "",
        branchId: port.branchId ?? null,
        isActive: port.isActive ?? true,
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
      // A branch list that fails to load must not block creating a port; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  private toFormValue(): PortFormValue {
    const value = this.model();

    return {
      // Blank means "allocate one from the Terminal number series" - only meaningful on create.
      code: value.code.trim().toUpperCase() || null,
      name: value.name.trim(),
      countryCode: value.countryCode.toUpperCase(),
      type: value.type,
      address: value.address,
      notes: value.notes.trim() || null,
      branchId: value.branchId,
      isActive: value.isActive,
    };
  }
}
