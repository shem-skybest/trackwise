import { Component, DestroyRef, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { form, FormField, FormRoot, max, min, required } from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import {
  Api,
  createEmployeeMasterData,
  getBranches,
  type CreateEmployeeCommand as CreateEmployeeMasterDataCommand,
  type RoleDto,
  type SalaryType,
  type UserDto,
} from "@trackwise/shared/api";
import { salaryTypeOptions } from "@trackwise/shared/api/enums";
import {
  Container,
  Icon,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import {
  CurrencyField,
  PageHeader,
  UiAutocompleteField,
  UiFormField,
  UiSelectField,
  UnitField,
  ValidatedForm,
} from "@/shared/components";
import { UserService } from "../services";

interface EmployeeAddModel {
  user: UserDto | null;
  role: RoleDto | null;
  salary: number;
  salaryType: SalaryType;
  /** Owning branch, or null when every branch uses this employee record. */
  branchId: string | null;
}

const EMPTY: EmployeeAddModel = {
  user: null,
  role: null,
  salary: 0,
  salaryType: "none",
  branchId: null,
};

@Component({
  selector: "app-employee-add",
  templateUrl: "./employee-add.html",
  imports: [
    Container,
    CurrencyField,
    FormField,
    FormRoot,
    Icon,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    Surface,
    Typography,
    UiAutocompleteField,
    UiButton,
    UiFormField,
    UiSelectField,
    UnitField,
    ValidatedForm,
  ],
})
export class EmployeeAdd {
  protected readonly salaryTypes = salaryTypeOptions;

  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly userService = inject(UserService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly suggestedUsers = signal<UserDto[]>([]);
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly isLoading = signal<boolean>(false);

  /**
   * "Every branch" is a real choice, not an empty one: an employee several offices share is
   * scoped to none of them rather than duplicated per office.
   */
  protected readonly branchOptions = signal<{ label: string; value: string | null }[]>([
    { label: "Every branch", value: null },
  ]);

  protected readonly model = signal<EmployeeAddModel>({ ...EMPTY });

  /** `isLoading` is retained separately because it tracks the initial roles load, not the submit. */
  protected readonly form = form(
    this.model,
    (p) => {
      required(p.user, { message: "Select a user." });
      required(p.salaryType, { message: "Salary type is required." });
      required(p.salary, { message: "Salary is required." });
      min(p.salary, 0, { message: "Salary cannot be negative." });
      max(p.salary, 100, {
        when: ({ valueOf }) => valueOf(p.salaryType) === "share_of_gross",
        message: "Share of gross cannot exceed 100%.",
      });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const user = v.user!;

          const newEmployee: CreateEmployeeMasterDataCommand = {
            userId: user.id ?? undefined,
            role: v.role?.name,
            salary: v.salary,
            salaryType: v.salaryType,
            branchId: v.branchId,
          };

          await this.api.invoke(createEmployeeMasterData, { body: newEmployee });
          this.toastService.showSuccess("New employee has been added successfully");
          this.form().reset({ ...EMPTY });
          return undefined;
        },
      },
    },
  );

  constructor() {
    this.fetchRoles();
    void this.loadBranches();
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
      // A branch list that fails to load must not block adding an employee; "Every branch" is a
      // legitimate answer and stays selectable.
    }
  }

  searchUser(event: { query: string }): void {
    this.userService
      .searchUser(event.query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((users) => {
        if (users) {
          this.suggestedUsers.set(users);
        }
      });
  }

  clearSelectedRole(): void {
    this.model.update((m) => ({ ...m, role: null }));
  }

  isShareOfGrossSalary(): boolean {
    return this.model().salaryType === "share_of_gross";
  }

  isNoneSalary(): boolean {
    return this.model().salaryType === "none";
  }

  private fetchRoles(): void {
    this.isLoading.set(true);

    this.userService
      .fetchRoles()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((roles) => {
        if (roles) {
          this.roles.set(roles);
        }

        this.isLoading.set(false);
      });
  }
}
