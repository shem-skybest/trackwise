import { Component, computed, effect, inject, input, model, output, signal } from "@angular/core";
import {
  disabled,
  form,
  FormField,
  FormRoot,
  max,
  maxLength,
  min,
  required,
} from "@angular/forms/signals";
import { DRIVING_ROLES, regionAllowedCountries, UserRole } from "@trackwise/shared";
import {
  Api,
  getBranches,
  updateEmployeeMasterData,
  type Address,
  type CrewUnit,
  type EmployeeDto as EmployeeMasterDataDto,
  type EmployeeStatus,
  type EmploymentType,
  type SalaryType,
  type UpdateEmployeeCommand as UpdateEmployeeMasterDataCommand,
} from "@trackwise/shared/api";
import {
  crewUnitOptions,
  employeeStatusOptions,
  employmentTypeOptions,
  salaryTypeOptions,
} from "@trackwise/shared/api/enums";
import {
  AddressForm,
  Stack,
  UiAccordionImports,
  UiButton,
  UiDialog,
  UiTextField,
  UiToggleField,
} from "@trackwise/shared/ui";
import { AuthService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { TenantService } from "@/core/services/tenant.service";
import {
  CurrencyField,
  UiFormField,
  UiSelectField,
  UnitField,
  ValidatedForm,
} from "@/shared/components";
import { NumberUtils } from "@/shared/utils";
import { ChangeRoleDialog } from "../change-role-dialog/change-role-dialog";

interface EmployeeEditModel {
  jobTitle: string;
  salary: number;
  salaryType: SalaryType;
  status: EmployeeStatus;
  address: Address | null;
  branchId: string | null;
  isActive: boolean;
  employmentType: EmploymentType | null;
  homeUnit: CrewUnit | null;
}

const EMPTY: EmployeeEditModel = {
  jobTitle: "",
  salary: 0,
  salaryType: "none",
  status: "active",
  address: null,
  branchId: null,
  isActive: true,
  employmentType: null,
  homeUnit: null,
};

/**
 * The API's answer when the dependency probe (or the live-login guard) stops a write. It carries
 * the message the steward needs, so it is shown verbatim rather than replaced with something
 * generic - same pattern as `truck-edit.ts`'s `conflictMessage`.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-employee-edit-dialog",
  templateUrl: "./employee-edit-dialog.html",
  imports: [
    AddressForm,
    ChangeRoleDialog,
    CurrencyField,
    FormField,
    FormRoot,
    Stack,
    UiAccordionImports,
    UiButton,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTextField,
    UiToggleField,
    UnitField,
    ValidatedForm,
  ],
})
export class EmployeeEditDialog {
  private readonly api = inject(Api);
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly toastService = inject(ToastService);

  protected readonly allowedCountries = computed(() =>
    regionAllowedCountries(this.tenantService.tenantData()?.settings?.region),
  );

  readonly visible = model<boolean>(false);
  readonly employee = input<EmployeeMasterDataDto | null>(null);
  readonly saved = output<void>();
  readonly deleted = output<void>();

  protected readonly salaryTypes = salaryTypeOptions;
  protected readonly statusOptions = employeeStatusOptions;
  protected readonly employmentTypes = employmentTypeOptions;
  protected readonly crewUnits = crewUnitOptions;

  /** Employment type and home unit describe crew, so they're shown for drivers only. */
  protected readonly isDriver = computed(() => {
    const role = this.employee()?.role?.name;
    return !!role && (DRIVING_ROLES as readonly string[]).includes(role);
  });
  protected readonly canChangeRole = signal(false);
  protected readonly changeRoleDialogVisible = signal(false);

  /**
   * "Every branch" is a real choice, not an empty one: an employee several offices share is
   * scoped to none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<EmployeeEditModel>({ ...EMPTY });

  /** The salary control's validators and disabled state are declarative, keyed off `salaryType`. */
  protected readonly form = form(
    this.model,
    (p) => {
      maxLength(p.jobTitle, 100, { message: "Job title cannot exceed 100 characters." });
      required(p.status, { message: "Status is required." });
      required(p.salaryType, { message: "Salary type is required." });
      required(p.salary, { message: "Salary is required." });
      min(p.salary, 0, { message: "Salary cannot be negative." });
      max(p.salary, 100, {
        when: ({ valueOf }) => valueOf(p.salaryType) === "share_of_gross",
        message: "Share of gross cannot exceed 100%.",
      });
      disabled(p.salary, { when: ({ valueOf }) => valueOf(p.salaryType) === "none" });
    },
    {
      submission: {
        action: async () => {
          const emp = this.employee();
          if (!emp?.id) {
            return undefined;
          }

          const v = this.model();
          const salaryType = v.salaryType;

          const command: UpdateEmployeeMasterDataCommand = {
            // "none" salary type carries no salary: reactive forms excluded the disabled control
            // from the payload, so send undefined to preserve that behavior.
            salary:
              salaryType === "none"
                ? undefined
                : salaryType === "share_of_gross"
                  ? NumberUtils.toRatio(v.salary)
                  : v.salary,
            salaryType,
            jobTitle: v.jobTitle.trim(),
            status: v.status,
            address: v.address ?? undefined,
            branchId: v.branchId,
            isActive: v.isActive,
            employmentType: v.employmentType ?? undefined,
            homeUnit: v.homeUnit ?? undefined,
          };

          try {
            await this.api.invoke(updateEmployeeMasterData, {
              id: emp.id,
              body: command,
            });
            this.saved.emit();
          } catch (error) {
            this.toastService.showError(
              this.conflictMessage(error) ??
                "Failed to update employee. Please check the information and try again.",
            );
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.loadBranches();

    effect(() => {
      const emp = this.employee();
      if (emp && this.visible()) {
        this.populateForm(emp);
        this.evaluateCanChangeRole(emp);
      }
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
      // A branch list that fails to load must not block editing an employee; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  /**
   * The dependency probe's - or the live-login guard's - own message, which names what is still
   * using the employee or why the write was refused. Shown verbatim rather than replaced with
   * something generic; overriding it is deliberately API-only, same as every other master entity.
   */
  private conflictMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }

  close(): void {
    this.visible.set(false);
  }

  openChangeRoleDialog(): void {
    this.changeRoleDialogVisible.set(true);
  }

  onRoleChanged(): void {
    this.changeRoleDialogVisible.set(false);
    this.saved.emit();
  }

  isShareOfGrossSalary(): boolean {
    return this.model().salaryType === "share_of_gross";
  }

  isNoneSalary(): boolean {
    return this.model().salaryType === "none";
  }

  private populateForm(emp: EmployeeMasterDataDto): void {
    const salaryType = emp.salaryType ?? "none";
    const salary = emp.salary ?? 0;

    this.model.set({
      jobTitle: emp.jobTitle ?? "",
      salary: salaryType === "share_of_gross" ? NumberUtils.toPercent(salary) : salary,
      salaryType,
      status: emp.status ?? "active",
      address: emp.address ?? null,
      branchId: emp.branchId ?? null,
      isActive: emp.isActive ?? true,
      employmentType: emp.employmentType ?? null,
      homeUnit: emp.homeUnit ?? null,
    });
  }

  private evaluateCanChangeRole(emp: EmployeeMasterDataDto): void {
    const user = this.authService.getUserData();
    const userRole = user?.role;
    const employeeRole = emp.role?.name;

    if (!userRole) {
      this.canChangeRole.set(false);
      return;
    }

    if (!employeeRole) {
      this.canChangeRole.set(true);
      return;
    }

    if (userRole === UserRole.AppSuperAdmin || userRole === UserRole.AppAdmin) {
      this.canChangeRole.set(true);
    } else if (userRole === UserRole.Owner && employeeRole !== UserRole.Owner) {
      this.canChangeRole.set(true);
    } else if (
      userRole === UserRole.Manager &&
      employeeRole !== UserRole.Owner &&
      employeeRole !== UserRole.Manager
    ) {
      this.canChangeRole.set(true);
    } else {
      this.canChangeRole.set(false);
    }
  }
}
