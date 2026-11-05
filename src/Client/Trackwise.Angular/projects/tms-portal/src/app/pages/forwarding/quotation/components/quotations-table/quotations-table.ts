import { DecimalPipe, TitleCasePipe, UpperCasePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { QuotationStatus, QuotationSummaryDto } from "@trackwise/shared/api";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Card, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-quotations-table",
  templateUrl: "./quotations-table.html",
  imports: [
    Card,
    DataContainer,
    DateFormatPipe,
    DecimalPipe,
    TitleCasePipe,
    UpperCasePipe,
    RouterLink,
    UiDataTable,
    UiSortHeader,
  ],
})
export class QuotationsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<QuotationSummaryDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(10);
  public readonly first = input(0);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addQuotation = output<void>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddQuotation(): void {
    this.addQuotation.emit();
  }

  protected navigateToQuotation(quotation: QuotationSummaryDto): void {
    if (quotation.id) {
      this.router.navigateByUrl(`/quotations/${quotation.id}`);
    }
  }

  protected statusBadgeClass(status?: QuotationStatus): string {
    switch (status) {
      case "accepted":
      case "approved":
        return "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400";
      case "issued":
        return "bg-blue-500/10 text-blue-600 dark:text-blue-400";
      case "pending_approval":
        return "bg-amber-500/10 text-amber-600 dark:text-amber-400";
      case "rejected":
      case "auto_declined":
      case "expired":
        return "bg-destructive/10 text-destructive";
      case "draft":
      default:
        return "bg-muted text-muted-foreground";
    }
  }

  protected formatStatus(status?: QuotationStatus): string {
    if (!status) return "-";
    return status.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
