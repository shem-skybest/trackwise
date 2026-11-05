import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Permission, PermissionGuard } from "@trackwise/shared";
import {
  Api,
  restoreEmployee,
  type EmployeeDto as EmployeeMasterDataDto,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Badge,
  Card,
  Stack,
  UiButton,
  UiDataTable,
  UiMenu,
  UiSelectField,
  UiSortHeader,
  type UiBadgeIntent,
  type UiMenuItem,
} from "@trackwise/shared/ui";
import { DataContainer, PageHeader, SearchField } from "@/shared/components";
import { EmployeeStatusTag } from "@/shared/components/tags";
import { EmployeeAvatar, InviteEmployeeDialog } from "../components";
import { EmployeesListStore } from "../store";

const DELETED_OPTIONS = [
  { label: "Hide deleted", value: false },
  { label: "Include deleted", value: true },
];

@Component({
  selector: "app-employees-list",
  templateUrl: "./employees-list.html",
  providers: [EmployeesListStore],
  imports: [
    Badge,
    Card,
    DataContainer,
    DatePipe,
    EmployeeAvatar,
    EmployeeStatusTag,
    InviteEmployeeDialog,
    PageHeader,
    PermissionGuard,
    RouterLink,
    SearchField,
    Stack,
    UiButton,
    UiDataTable,
    UiMenu,
    UiSelectField,
    UiSortHeader,
  ],
})
export class EmployeeList {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  protected readonly store = inject(EmployeesListStore);
  protected readonly Permission = Permission;

  protected readonly inviteDialogVisible = signal(false);
  protected readonly selectedRow = signal<EmployeeMasterDataDto | null>(null);
  protected readonly includeDeleted = signal(false);
  protected readonly deletedOptions = DELETED_OPTIONS;

  /**
   * A deleted employee has no edit form (it is filtered out of every read but the master-data
   * list itself), so its only action is Restore - mirrors `PortsTable`'s row menu.
   */
  protected readonly actionMenuItems = computed<UiMenuItem[]>(() => {
    const row = this.selectedRow();
    if (row?.deletedAt) {
      return [
        {
          label: "Restore",
          icon: "history",
          command: () => this.restore(row),
        },
      ];
    }

    return [
      {
        label: "View details",
        icon: "eye",
        command: () => this.router.navigateByUrl(`/master-data/employees/${row!.id}`),
      },
      {
        label: "View payrolls",
        icon: "file",
        command: () => this.router.navigateByUrl(`/payroll/employee/${row!.id}`),
      },
      {
        label: "View timesheets",
        icon: "clock",
        command: () => this.router.navigateByUrl(`/timesheets/employee/${row!.id}`),
      },
    ];
  });

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(employee: EmployeeMasterDataDto): Promise<void> {
    if (!employee.id) {
      return;
    }

    try {
      await this.api.invoke(restoreEmployee, { id: employee.id });
      this.toastService.showSuccess(`Employee ${employee.fullName} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore employee.");
    }
  }

  protected getRoleSeverity(roleName: string | undefined): UiBadgeIntent {
    switch (roleName?.toLowerCase()) {
      case "owner":
        return "warn";
      case "manager":
        return "info";
      case "dispatcher":
        return "secondary";
      case "driver":
        return "success";
      default:
        return "secondary";
    }
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected addEmployee(): void {
    this.router.navigate(["/master-data/employees/add"]);
  }

  protected openInviteDialog(): void {
    this.inviteDialogVisible.set(true);
  }

  protected onInvitationSent(): void {
    // Optionally refresh the list or navigate to pending invitations
  }
}
