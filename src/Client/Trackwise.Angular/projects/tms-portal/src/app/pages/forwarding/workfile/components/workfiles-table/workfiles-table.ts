import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { WorkfileStatus, WorkfileSummaryDto, WorkfileType } from "@trackwise/shared/api";
import { workfileStatusOptions, workfileTypeOptions } from "@trackwise/shared/api/enums";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiDataTable, UiSortHeader, type UiBadgeIntent } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const TYPE_LABELS = new Map(workfileTypeOptions.map((o) => [o.value, o.label]));
const STATUS_LABELS = new Map(workfileStatusOptions.map((o) => [o.value, o.label]));

const STATUS_INTENT: Record<string, UiBadgeIntent> = {
  open: "info",
  manager_review: "warn",
  accounting_review: "warn",
  closed: "success",
  cancelled: "danger",
};

@Component({
  selector: "app-workfiles-table",
  templateUrl: "./workfiles-table.html",
  imports: [Badge, Card, DataContainer, DateFormatPipe, RouterLink, UiDataTable, UiSortHeader],
})
export class WorkfilesTable {
  private readonly router = inject(Router);

  public readonly data = input.required<WorkfileSummaryDto[]>();
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

  protected navigateToWorkfile(workfile: WorkfileSummaryDto): void {
    if (workfile.id) {
      this.router.navigateByUrl(`/forwarding/workfiles/${workfile.id}`);
    }
  }

  protected typeLabel(type: WorkfileType): string {
    return TYPE_LABELS.get(type) ?? type;
  }

  protected statusLabel(status: WorkfileStatus): string {
    return STATUS_LABELS.get(status) ?? status;
  }

  protected statusIntent(status: WorkfileStatus): UiBadgeIntent {
    return STATUS_INTENT[status] ?? "secondary";
  }
}
