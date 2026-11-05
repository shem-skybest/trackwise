import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { GensetDto, GensetStatus } from "@trackwise/shared/api";
import { gensetStatusOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import {
  Badge,
  Card,
  UiButton,
  UiDataTable,
  UiSortHeader,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const STATUS_LABELS = new Map(gensetStatusOptions.map((option) => [option.value, option.label]));

const STATUS_SEVERITY: Record<GensetStatus, UiBadgeIntent> = {
  available: "success",
  in_use: "info",
  under_maintenance: "warn",
  out_of_service: "danger",
};

@Component({
  selector: "app-gensets-table",
  templateUrl: "./gensets-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class GensetsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<GensetDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addGenset = output<void>();
  public readonly restoreGenset = output<GensetDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddGenset(): void {
    this.addGenset.emit();
  }

  protected statusLabel(status: GensetStatus | undefined): string {
    return status ? (STATUS_LABELS.get(status) ?? "—") : "—";
  }

  protected statusSeverity(status: GensetStatus | undefined): UiBadgeIntent {
    return status ? (STATUS_SEVERITY[status] ?? "secondary") : "secondary";
  }

  protected navigateToGenset(genset: GensetDto): void {
    // A deleted genset has no edit form - restore is the only action available on it.
    if (genset.id && !genset.deletedAt) {
      this.router.navigateByUrl(`/master-data/gensets/${genset.id}`);
    }
  }

  protected onRestore(genset: GensetDto, event: Event): void {
    event.stopPropagation();
    this.restoreGenset.emit(genset);
  }
}
