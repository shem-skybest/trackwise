import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { CommodityDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-commodities-table",
  templateUrl: "./commodities-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class CommoditiesTable {
  private readonly router = inject(Router);

  public readonly data = input.required<CommodityDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addCommodity = output<void>();
  public readonly restoreCommodity = output<CommodityDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddCommodity(): void {
    this.addCommodity.emit();
  }

  protected navigateToCommodity(commodity: CommodityDto): void {
    // A deleted commodity has no edit form - restore is the only action available on it.
    if (commodity.id && !commodity.deletedAt) {
      this.router.navigateByUrl(`/master-data/commodities/${commodity.id}`);
    }
  }

  protected onRestore(commodity: CommodityDto, event: Event): void {
    event.stopPropagation();
    this.restoreCommodity.emit(commodity);
  }
}
