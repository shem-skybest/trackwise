import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { LocationDto, LocationType } from "@trackwise/shared/api";
import { locationTypeOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, UiButton, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";
import { formatTimeOfDay } from "@/shared/utils";

const TYPE_LABELS = new Map(locationTypeOptions.map((option) => [option.value, option.label]));

@Component({
  selector: "app-locations-table",
  templateUrl: "./locations-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class LocationsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<LocationDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addLocation = output<void>();
  public readonly restoreLocation = output<LocationDto>();

  protected readonly formatTimeOfDay = formatTimeOfDay;

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddLocation(): void {
    this.addLocation.emit();
  }

  protected typeLabel(type: LocationType | undefined): string {
    return type ? (TYPE_LABELS.get(type) ?? "—") : "—";
  }

  /** A zone has no address of its own, so the column shows what it is instead of an empty cell. */
  protected place(location: LocationDto): string {
    const address = location.address;

    if (!address) {
      return "";
    }

    return [address.cityOrMunicipality, address.province].filter(Boolean).join(", ");
  }

  protected navigateToLocation(location: LocationDto): void {
    // A deleted location has no edit form - restore is the only action available on it.
    if (location.id && !location.deletedAt) {
      this.router.navigateByUrl(`/master-data/locations/${location.id}`);
    }
  }

  protected onRestore(location: LocationDto, event: Event): void {
    event.stopPropagation();
    this.restoreLocation.emit(location);
  }
}
