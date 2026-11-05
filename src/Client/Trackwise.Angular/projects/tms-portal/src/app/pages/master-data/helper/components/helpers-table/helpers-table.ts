import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { HelperDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-helpers-table",
  templateUrl: "./helpers-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class HelpersTable {
  private readonly router = inject(Router);

  public readonly data = input.required<HelperDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addHelper = output<void>();
  public readonly restoreHelper = output<HelperDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddHelper(): void {
    this.addHelper.emit();
  }

  protected navigateToHelper(helper: HelperDto): void {
    // A deleted helper has no edit form - restore is the only action available on it.
    if (helper.id && !helper.deletedAt) {
      this.router.navigateByUrl(`/master-data/helpers/${helper.id}`);
    }
  }

  protected onRestore(helper: HelperDto, event: Event): void {
    event.stopPropagation();
    this.restoreHelper.emit(helper);
  }
}
