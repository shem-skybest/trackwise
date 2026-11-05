import { Component, DestroyRef, inject, signal, type OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormsModule } from "@angular/forms";
import { Permission, PermissionService, UiMultiSelectField } from "@trackwise/shared";
import type { InvoiceStatus } from "@trackwise/shared/api";
import { PageHeader } from "@trackwise/shared/ui/containers/page-header/page-header";
import { debounceTime, distinctUntilChanged, Subject } from "rxjs";
import { BillingTable } from "../components/billing-table/billing-table";
import { BillingListStore } from "../store/billing-list.store";

const INVOICE_STATUS_OPTIONS: { label: string; value: InvoiceStatus }[] = [
  { label: "Draft", value: "draft" },
  { label: "Issued", value: "issued" },
  { label: "Paid", value: "paid" },
  { label: "Overdue", value: "overdue" },
  { label: "Cancelled", value: "cancelled" },
];

@Component({
  selector: "app-billing-list",
  templateUrl: "./billing-list.component.html",
  providers: [BillingListStore],
  imports: [PageHeader, BillingTable, FormsModule, UiMultiSelectField],
})
export class BillingListComponent implements OnInit {
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(BillingListStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Billing.Manage);
  protected readonly statusOptions = INVOICE_STATUS_OPTIONS;
  protected readonly selectedStatuses = signal<InvoiceStatus[]>([]);
  private readonly destroyRef = inject(DestroyRef);
  private readonly search$ = new Subject<string>();

  ngOnInit(): void {
    this.store.setFilters({
      InvoiceType: "Booking",
    });

    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => this.store.setFilters({ Search: search || undefined }));
  }

  protected onSearchChange(search: string): void {
    this.search$.next(search.trim());
  }

  protected onStatusChange(statuses: InvoiceStatus[]): void {
    this.selectedStatuses.set(statuses);
    this.store.setFilters({ Status: statuses.length ? statuses : undefined });
  }

  protected onCustomerChange(customerId?: string): void {
    this.store.setFilters({ CustomerId: customerId });
  }
}
