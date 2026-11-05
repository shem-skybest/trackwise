import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Api, restoreCurrency, type CurrencyDto } from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { Card, Grid, Stack, UiFormField, UiSelectField, UiTextField } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { CurrenciesTable } from "../components/currencies-table/currencies-table";
import { CurrenciesListStore } from "../store/currencies-list.store";

@Component({
  selector: "app-currencies-list",
  templateUrl: "./currencies-list.html",
  providers: [CurrenciesListStore],
  imports: [
    CurrenciesTable,
    Card,
    Grid,
    PageHeader,
    Stack,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class CurrenciesListComponent {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(CurrenciesListStore);

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

  protected addCurrency(): void {
    void this.router.navigateByUrl("/master-data/currencies/add");
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

  protected async restore(currency: CurrencyDto): Promise<void> {
    if (!currency.id) {
      return;
    }

    try {
      await this.api.invoke(restoreCurrency, { id: currency.id });
      this.toastService.showSuccess(`Currency ${currency.code} restored`);
      this.store.retry();
    } catch {
      this.toastService.showError("Failed to restore currency.");
    }
  }
}
