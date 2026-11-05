import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { CurrencyDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-currencies-table",
  templateUrl: "./currencies-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class CurrenciesTable {
  private readonly router = inject(Router);

  public readonly data = input.required<CurrencyDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addCurrency = output<void>();
  public readonly restoreCurrency = output<CurrencyDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddCurrency(): void {
    this.addCurrency.emit();
  }

  protected navigateToCurrency(currency: CurrencyDto): void {
    // A deleted currency has no edit form - restore is the only action available on it.
    if (currency.id && !currency.deletedAt) {
      this.router.navigateByUrl(`/master-data/currencies/${currency.id}`);
    }
  }

  protected onRestore(currency: CurrencyDto, event: Event): void {
    event.stopPropagation();
    this.restoreCurrency.emit(currency);
  }
}
