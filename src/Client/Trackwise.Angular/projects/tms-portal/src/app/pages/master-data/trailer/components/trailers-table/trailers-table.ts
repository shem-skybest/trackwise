import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { TrailerDto, TrailerType } from "@trackwise/shared/api";
import { trailerTypeOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const TYPE_LABELS = new Map(trailerTypeOptions.map((option) => [option.value, option.label]));

@Component({
  selector: "app-trailers-table",
  templateUrl: "./trailers-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class TrailersTable {
  private readonly router = inject(Router);

  public readonly data = input.required<TrailerDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addTrailer = output<void>();
  public readonly restoreTrailer = output<TrailerDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddTrailer(): void {
    this.addTrailer.emit();
  }

  protected typeLabel(type: TrailerType | undefined): string {
    return type ? (TYPE_LABELS.get(type) ?? "—") : "—";
  }

  protected navigateToTrailer(trailer: TrailerDto): void {
    // A deleted trailer has no edit form - restore is the only action available on it.
    if (trailer.id && !trailer.deletedAt) {
      this.router.navigateByUrl(`/master-data/trailers/${trailer.id}`);
    }
  }

  protected onRestore(trailer: TrailerDto, event: Event): void {
    event.stopPropagation();
    this.restoreTrailer.emit(trailer);
  }
}
