import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreVessel, type VesselDto } from "@trackwise/shared/api";
import { vesselTypeOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { VesselsTable } from "../components/vessels-table/vessels-table";
import { VesselsListStore } from "../store/vessels-list.store";

@Component({
  selector: "app-vessels-list",
  templateUrl: "./vessels-list.html",
  providers: [VesselsListStore],
  imports: [VesselsTable, Card, Grid, PageHeader, Stack, UiFormField, UiSelectField, UiTextField],
})
export class VesselsListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(VesselsListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly typeOptions = [{ label: "All Types", value: null }, ...vesselTypeOptions];

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

  protected addVessel(): void {
    void this.router.navigateByUrl("/master-data/vessels/add");
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

  protected async restore(vessel: VesselDto): Promise<void> {
    if (!vessel.id) {
      return;
    }

    try {
      await this.api.invoke(restoreVessel, { id: vessel.id });
      this.toastService.showSuccess(`Vessel ${vessel.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore vessel.");
    }
  }
}
