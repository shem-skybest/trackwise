import { Component, computed, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  restoreTruck,
  type Address,
  type TruckDto as TruckMasterDataDto,
} from "@trackwise/shared/api";
import { AddressPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Stack,
  UiButton,
  UiDataTable,
  UiMenu,
  UiSelectField,
  UiSortHeader,
  UiTooltip,
  type UiMenuItem,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { DataContainer, PageHeader, TruckStatusTag, TruckTypeTag } from "@/shared/components";
import {
  TrucksFilterPanel,
  type TrucksFilterState,
} from "../components/trucks-filter-panel/trucks-filter-panel";
import { TrucksMapView } from "../components/trucks-map-view/trucks-map-view";
import { TrucksSummaryStats } from "../components/trucks-summary-stats/trucks-summary-stats";
import { TrucksListStore } from "../store/trucks-list.store";

const DELETED_OPTIONS = [
  { label: "Hide deleted", value: false },
  { label: "Include deleted", value: true },
];

@Component({
  selector: "app-trucks-list",
  templateUrl: "./trucks-list.html",
  providers: [TrucksListStore, AddressPipe],
  imports: [
    AddressPipe,
    Badge,
    Card,
    DataContainer,
    PageHeader,
    Stack,
    TrucksFilterPanel,
    TrucksMapView,
    TrucksSummaryStats,
    TruckStatusTag,
    TruckTypeTag,
    UiButton,
    UiDataTable,
    UiMenu,
    UiSelectField,
    UiSortHeader,
    UiTooltip,
  ],
})
export class TrucksList {
  private readonly router = inject(Router);
  private readonly addressPipe = inject(AddressPipe);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  protected readonly store = inject(TrucksListStore);

  protected readonly selectedRow = signal<TruckMasterDataDto | null>(null);
  protected readonly viewMode = signal<"table" | "map">("table");
  protected readonly includeDeleted = signal(false);
  protected readonly deletedOptions = DELETED_OPTIONS;

  /**
   * A deleted truck has no edit form (it is filtered out of every read but the master-data list
   * itself), so its only action is Restore - mirrors `PortsTable`'s row menu.
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
        command: () => this.router.navigateByUrl(`/master-data/trucks/${row!.id}`),
      },
      {
        label: "Edit truck",
        icon: "pencil",
        command: () => this.router.navigateByUrl(`/master-data/trucks/${row!.id}/edit`),
      },
      {
        label: "Manage documents",
        icon: "folder",
        command: () => this.router.navigateByUrl(`/master-data/trucks/${row!.id}/documents`),
      },
    ];
  });

  protected onSearchChanged(value: string): void {
    this.store.setSearch(value);
  }

  protected onFiltersChanged(filters: TrucksFilterState): void {
    this.store.setFilters({
      Statuses: filters.statuses.length > 0 ? filters.statuses : undefined,
      Types: filters.types.length > 0 ? filters.types : undefined,
      MissingGrossVehicleWeight: filters.missingGrossVehicleWeight || undefined,
    });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected addTruck(): void {
    this.router.navigate(["/master-data/trucks/add"]);
  }

  protected formatAddress(address: Address): string {
    return this.addressPipe.transform(address) || "No address provided";
  }

  protected async restore(truck: TruckMasterDataDto): Promise<void> {
    if (!truck.id) {
      return;
    }

    try {
      await this.api.invoke(restoreTruck, { id: truck.id });
      this.toastService.showSuccess(`Truck ${truck.number} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore truck.");
    }
  }
}
