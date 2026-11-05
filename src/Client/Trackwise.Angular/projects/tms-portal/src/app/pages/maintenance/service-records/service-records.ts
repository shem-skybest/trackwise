import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { MaintenanceRecordDto, MaintenanceType, VehicleSystem } from "@trackwise/shared/api";
import { maintenanceTypeOptions, vehicleSystemOptions } from "@trackwise/shared/api/enums";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Grid,
  UiButton,
  UiDataTable,
  UiFormField,
  UiMenu,
  UiSelectField,
  UiSortHeader,
  type UiBadgeIntent,
  type UiMenuItem,
} from "@trackwise/shared/ui";
import { DataContainer, PageHeader, SearchField } from "@/shared/components";
import { ServiceRecordsStore } from "../store";

@Component({
  selector: "app-service-records",
  templateUrl: "./service-records.html",
  providers: [ServiceRecordsStore],
  imports: [
    Badge,
    Card,
    CurrencyFormatPipe,
    DataContainer,
    DatePipe,
    Grid,
    PageHeader,
    RouterLink,
    SearchField,
    UiButton,
    UiDataTable,
    UiFormField,
    UiMenu,
    UiSelectField,
    UiSortHeader,
  ],
})
export class ServiceRecordsPage {
  private readonly router = inject(Router);
  protected readonly store = inject(ServiceRecordsStore);

  protected readonly typeOptions = maintenanceTypeOptions;
  protected readonly systemOptions = vehicleSystemOptions;
  protected readonly type = signal<MaintenanceType | null>(null);
  protected readonly system = signal<VehicleSystem | null>(null);
  protected readonly selectedRow = signal<MaintenanceRecordDto | null>(null);

  protected readonly actionMenuItems: UiMenuItem[] = [
    {
      label: "View details",
      icon: "eye",
      command: () => this.viewDetails(this.selectedRow()!),
    },
    {
      label: "Edit",
      icon: "pencil",
      command: () =>
        this.router.navigateByUrl(`/maintenance/records/${this.selectedRow()!.id}/edit`),
    },
  ];

  protected systemLabel(system: VehicleSystem | null | undefined): string {
    return vehicleSystemOptions.find((o) => o.value === system)?.label ?? "-";
  }

  protected typeIntent(type: MaintenanceType | undefined): UiBadgeIntent {
    switch (type) {
      case "repair":
        return "warn";
      case "reefer_pms":
      case "preventive_maintenance":
      case "oil_change":
        return "info";
      default:
        return "secondary";
    }
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onTypeChange(type: MaintenanceType | null): void {
    this.type.set(type);
    this.store.setFilters({ Type: type ?? undefined });
  }

  protected onSystemChange(system: VehicleSystem | null): void {
    this.system.set(system);
    this.store.setFilters({ VehicleSystem: system ?? undefined });
  }

  protected addRecord(): void {
    this.router.navigate(["/maintenance/records/add"]);
  }

  protected viewDetails(record: MaintenanceRecordDto): void {
    this.router.navigateByUrl(`/maintenance/records/${record.id}`);
  }
}
