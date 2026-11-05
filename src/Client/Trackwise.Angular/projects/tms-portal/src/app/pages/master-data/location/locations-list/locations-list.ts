import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreLocation, type CustomerDto, type LocationDto } from "@trackwise/shared/api";
import { locationTypeOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { SearchCustomer } from "@/shared/components/search";
import { LocationsTable } from "../components/locations-table/locations-table";
import { LocationsListStore } from "../store/locations-list.store";

@Component({
  selector: "app-locations-list",
  templateUrl: "./locations-list.html",
  providers: [LocationsListStore],
  imports: [
    LocationsTable,
    Card,
    Grid,
    PageHeader,
    SearchCustomer,
    Stack,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class LocationsListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(LocationsListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly includeDeleted = signal(false);
  protected readonly selectedCustomer = signal<CustomerDto | null>(null);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly typeOptions = [{ label: "All Types", value: null }, ...locationTypeOptions];

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

  protected addLocation(): void {
    void this.router.navigateByUrl("/master-data/locations/add");
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

  protected onCustomerChange(customer: CustomerDto | null): void {
    this.selectedCustomer.set(customer);
    this.store.setFilters({ CustomerId: customer?.id ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(location: LocationDto): Promise<void> {
    if (!location.id) {
      return;
    }

    try {
      await this.api.invoke(restoreLocation, { id: location.id });
      this.toastService.showSuccess(`Location ${location.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore location.");
    }
  }
}
