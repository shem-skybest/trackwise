import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restorePort, type TerminalDto } from "@trackwise/shared/api";
import { terminalTypeOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { PortsTable } from "../components/ports-table/ports-table";
import { PortsListStore } from "../store/ports-list.store";

@Component({
  selector: "app-ports-list",
  templateUrl: "./ports-list.html",
  providers: [PortsListStore],
  imports: [PortsTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class PortsListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(PortsListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly typeOptions = [{ label: "All Types", value: null }, ...terminalTypeOptions];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters(
      { IsActive: undefined, Type: undefined, IncludeDeleted: false },
      { reload: false },
    );
  }

  protected addPort(): void {
    void this.router.navigateByUrl("/master-data/ports/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onStatusChange(value: boolean | null): void {
    this.selectedStatus.set(value);
    this.store.setFilters({ IsActive: value ?? undefined });
  }

  protected onTypeChange(value: string | null): void {
    this.selectedType.set(value);
    this.store.setFilters({ Type: value ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(port: TerminalDto): Promise<void> {
    if (!port.id) {
      return;
    }

    try {
      await this.api.invoke(restorePort, { id: port.id });
      this.toastService.showSuccess(`Port ${port.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore port.");
    }
  }
}
