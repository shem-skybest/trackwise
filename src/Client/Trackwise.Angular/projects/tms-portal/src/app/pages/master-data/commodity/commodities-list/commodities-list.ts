import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreCommodity, type CommodityDto } from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { CommoditiesTable } from "../components/commodities-table/commodities-table";
import { CommoditiesListStore } from "../store/commodities-list.store";

@Component({
  selector: "app-commodities-list",
  templateUrl: "./commodities-list.html",
  providers: [CommoditiesListStore],
  imports: [
    CommoditiesTable,
    Card,
    Grid,
    PageHeader,
    Stack,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class CommoditiesListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(CommoditiesListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly selectedHandling = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  /**
   * One filter over two independent flags rather than two selects. They are the questions a steward
   * actually asks - "what needs a reefer", "what is hazardous" - and a pair of tri-state dropdowns
   * would offer combinations nobody looks for.
   */
  protected readonly handlingOptions = [
    { label: "All handling", value: null },
    { label: "Hazardous", value: "hazardous" },
    { label: "Temperature controlled", value: "reefer" },
  ];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters(
      { IsActive: undefined, IsHazardous: undefined, IncludeDeleted: false },
      { reload: false },
    );
  }

  protected addCommodity(): void {
    void this.router.navigateByUrl("/master-data/commodities/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onStatusChange(value: boolean | null): void {
    this.selectedStatus.set(value);
    this.store.setFilters({ IsActive: value ?? undefined });
  }

  protected onHandlingChange(value: string | null): void {
    this.selectedHandling.set(value);

    this.store.setFilters({
      IsHazardous: value === "hazardous" ? true : undefined,
      RequiresTemperatureControl: value === "reefer" ? true : undefined,
    });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(commodity: CommodityDto): Promise<void> {
    if (!commodity.id) {
      return;
    }

    try {
      await this.api.invoke(restoreCommodity, { id: commodity.id });
      this.toastService.showSuccess(`Commodity ${commodity.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore commodity.");
    }
  }
}
