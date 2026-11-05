import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { TerminalDto, TerminalType } from "@trackwise/shared/api";
import { terminalTypeOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const TYPE_LABELS = new Map(terminalTypeOptions.map((option) => [option.value, option.label]));

@Component({
  selector: "app-ports-table",
  templateUrl: "./ports-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class PortsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<TerminalDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addPort = output<void>();
  public readonly restorePort = output<TerminalDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddPort(): void {
    this.addPort.emit();
  }

  protected typeLabel(type: TerminalType | undefined): string {
    return type ? (TYPE_LABELS.get(type) ?? "—") : "—";
  }

  protected navigateToPort(port: TerminalDto): void {
    // A deleted port has no edit form - restore is the only action available on it.
    if (port.id && !port.deletedAt) {
      this.router.navigateByUrl(`/master-data/ports/${port.id}`);
    }
  }

  protected onRestore(port: TerminalDto, event: Event): void {
    event.stopPropagation();
    this.restorePort.emit(port);
  }
}
