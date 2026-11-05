import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreBranch, type BranchDto } from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { BranchesTable } from "../components/branches-table/branches-table";
import { BranchesListStore } from "../store/branches-list.store";

@Component({
  selector: "app-branches-list",
  templateUrl: "./branches-list.html",
  providers: [BranchesListStore],
  imports: [BranchesTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class BranchesListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(BranchesListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters({ IsActive: undefined, IncludeDeleted: false }, { reload: false });
  }

  protected addBranch(): void {
    void this.router.navigateByUrl("/master-data/branches/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onStatusChange(value: boolean | null): void {
    this.selectedStatus.set(value);
    this.store.setFilters({ IsActive: value ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(branch: BranchDto): Promise<void> {
    if (!branch.id) {
      return;
    }

    try {
      await this.api.invoke(restoreBranch, { id: branch.id });
      this.toastService.showSuccess(`Branch ${branch.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore branch.");
    }
  }
}
