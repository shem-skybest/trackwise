import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreHelper, type HelperDto } from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { HelpersTable } from "../components/helpers-table/helpers-table";
import { HelpersListStore } from "../store/helpers-list.store";

@Component({
  selector: "app-helpers-list",
  templateUrl: "./helpers-list.html",
  providers: [HelpersListStore],
  imports: [HelpersTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class HelpersListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(HelpersListStore);

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

  protected addHelper(): void {
    void this.router.navigateByUrl("/master-data/helpers/add");
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

  protected async restore(helper: HelperDto): Promise<void> {
    if (!helper.id) {
      return;
    }

    try {
      await this.api.invoke(restoreHelper, { id: helper.id });
      this.toastService.showSuccess(`Helper ${helper.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore helper.");
    }
  }
}
