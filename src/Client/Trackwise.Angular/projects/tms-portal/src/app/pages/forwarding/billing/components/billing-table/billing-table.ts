import { CurrencyPipe, DatePipe, UpperCasePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { InvoiceDto, InvoiceStatus } from "@trackwise/shared/api";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Card, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-billing-table",
  templateUrl: "./billing-table.html",
  imports: [
    Card,
    CurrencyPipe,
    DataContainer,
    DatePipe,
    DateFormatPipe,
    UpperCasePipe,
    RouterLink,
    UiDataTable,
    UiSortHeader,
  ],
})
export class BillingTable {
  private readonly router = inject(Router);

  public readonly data = input.required<InvoiceDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(10);
  public readonly first = input(0);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected navigateToInvoice(invoice: InvoiceDto): void {
    if (invoice.id) {
      this.router.navigateByUrl(`/billing-invoicing/${invoice.id}`);
    }
  }

  protected invoiceStatusLabel(status?: InvoiceStatus | string): string {
    if (!status) return "—";
    return status.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
  }

  protected statusBadgeClass(status?: InvoiceStatus | string): string {
    switch (status?.toLowerCase()) {
      case "paid":
        return "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400";
      case "issued":
      case "sent":
        return "bg-blue-500/10 text-blue-600 dark:text-blue-400";
      case "overdue":
        return "bg-rose-500/10 text-rose-600 dark:text-rose-400";
      case "cancelled":
      case "void":
        return "bg-amber-500/10 text-amber-600 dark:text-amber-400";
      case "draft":
      default:
        return "bg-muted text-muted-foreground";
    }
  }
}
