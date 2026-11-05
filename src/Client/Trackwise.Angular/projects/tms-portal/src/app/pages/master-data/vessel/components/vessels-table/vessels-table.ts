import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { VesselDto, VesselType } from "@trackwise/shared/api";
import { vesselTypeOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const TYPE_LABELS = new Map(vesselTypeOptions.map((option) => [option.value, option.label]));

@Component({
  selector: "app-vessels-table",
  templateUrl: "./vessels-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class VesselsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<VesselDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addVessel = output<void>();
  public readonly restoreVessel = output<VesselDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddVessel(): void {
    this.addVessel.emit();
  }

  protected typeLabel(type: VesselType | undefined): string {
    return type ? (TYPE_LABELS.get(type) ?? "—") : "—";
  }

  protected navigateToVessel(vessel: VesselDto): void {
    // A deleted vessel has no edit form - restore is the only action available on it.
    if (vessel.id && !vessel.deletedAt) {
      this.router.navigateByUrl(`/master-data/vessels/${vessel.id}`);
    }
  }

  protected onRestore(vessel: VesselDto, event: Event): void {
    event.stopPropagation();
    this.restoreVessel.emit(vessel);
  }
}
