import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { ContainerTypeDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

@Component({
  selector: "app-container-types-table",
  templateUrl: "./container-types-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class ContainerTypesTable {
  private readonly router = inject(Router);

  public readonly data = input.required<ContainerTypeDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addContainerType = output<void>();
  public readonly restoreContainerType = output<ContainerTypeDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddContainerType(): void {
    this.addContainerType.emit();
  }

  protected navigateToContainerType(containerType: ContainerTypeDto): void {
    // A deleted type has no edit form - restore is the only action available on it.
    if (containerType.id && !containerType.deletedAt) {
      this.router.navigateByUrl(`/master-data/container-types/${containerType.id}`);
    }
  }

  protected onRestore(containerType: ContainerTypeDto, event: Event): void {
    event.stopPropagation();
    this.restoreContainerType.emit(containerType);
  }

  /** "40 ft HC", or a dash where the equipment genuinely has no dimensions. */
  protected size(containerType: ContainerTypeDto): string | null {
    if (containerType.lengthFeet == null) {
      return null;
    }

    return containerType.isHighCube
      ? `${containerType.lengthFeet} ft HC`
      : `${containerType.lengthFeet} ft`;
  }
}
