import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreContainerType, type ContainerTypeDto } from "@trackwise/shared/api";
import { containerTypeCategoryOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { ContainerTypesTable } from "../components/container-types-table/container-types-table";
import { ContainerTypesListStore } from "../store/container-types-list.store";

@Component({
  selector: "app-container-types-list",
  templateUrl: "./container-types-list.html",
  providers: [ContainerTypesListStore],
  imports: [
    ContainerTypesTable,
    Card,
    Grid,
    PageHeader,
    Stack,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class ContainerTypesListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(ContainerTypesListStore);

  protected readonly canManage = this.permissions.hasPermission(Permission.MasterData.Manage);

  protected readonly selectedStatus = signal<boolean | null>(null);
  protected readonly selectedCategory = signal<string | null>(null);
  protected readonly selectedLength = signal<number | null>(null);
  protected readonly includeDeleted = signal(false);

  protected readonly statusOptions = [
    { label: "All Statuses", value: null },
    { label: "Active", value: true },
    { label: "Inactive", value: false },
  ];

  protected readonly categoryOptions = [
    { label: "All categories", value: null },
    ...containerTypeCategoryOptions,
  ];

  protected readonly lengthOptions = [
    { label: "All sizes", value: null },
    { label: "20 ft", value: 20 },
    { label: "40 ft", value: 40 },
    { label: "45 ft", value: 45 },
  ];

  protected readonly deletedOptions = [
    { label: "Hide deleted", value: false },
    { label: "Include deleted", value: true },
  ];

  constructor() {
    // Seeded without a reload - the table fires its own initial lazy-load on mount, and reloading
    // here would make the page issue two identical requests.
    this.store.setFilters(
      { IsActive: undefined, Category: undefined, IncludeDeleted: false },
      { reload: false },
    );
  }

  protected addContainerType(): void {
    void this.router.navigateByUrl("/master-data/container-types/add");
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onStatusChange(value: boolean | null): void {
    this.selectedStatus.set(value);
    this.store.setFilters({ IsActive: value ?? undefined });
  }

  protected onCategoryChange(value: string | null): void {
    this.selectedCategory.set(value);
    this.store.setFilters({ Category: value ?? undefined });
  }

  protected onLengthChange(value: number | null): void {
    this.selectedLength.set(value);
    this.store.setFilters({ LengthFeet: value ?? undefined });
  }

  protected onIncludeDeletedChange(value: boolean | null): void {
    this.includeDeleted.set(value ?? false);
    this.store.setFilters({ IncludeDeleted: value ?? false });
  }

  protected async restore(containerType: ContainerTypeDto): Promise<void> {
    if (!containerType.id) {
      return;
    }

    try {
      await this.api.invoke(restoreContainerType, { id: containerType.id });
      this.toastService.showSuccess(`Container type ${containerType.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore container type.");
    }
  }
}
