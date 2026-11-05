import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreGenset, type GensetDto } from "@trackwise/shared/api";
import { gensetStatusOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { GensetsTable } from "../components/gensets-table/gensets-table";
import { GensetsListStore } from "../store/gensets-list.store";

@Component({
  selector: "app-gensets-list",
  templateUrl: "./gensets-list.html",
  providers: [GensetsListStore],
  imports: [GensetsTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class GensetsListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(GensetsListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedActive = signal<boolean | null>(null);
  protected readonly selectedStatus = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly activeOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly statusOptions = [{ label: "All", value: null }, ...gensetStatusOptions];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters(
      { IsActive: undefined, Status: undefined, IncludeDeleted: false },
      { reload: false },
    );
  }

  protected addGenset(): void {
    void this.router.navigateByUrl("/master-data/gensets/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onActiveChange(value: boolean | null): void {
    this.selectedActive.set(value);
    this.store.setFilters({ IsActive: value ?? undefined });
  }

  protected onStatusChange(value: string | null): void {
    this.selectedStatus.set(value);
    this.store.setFilters({ Status: value ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(genset: GensetDto): Promise<void> {
    if (!genset.id) {
      return;
    }

    try {
      await this.api.invoke(restoreGenset, { id: genset.id });
      this.toastService.showSuccess(`Genset ${genset.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore genset.");
    }
  }
}
