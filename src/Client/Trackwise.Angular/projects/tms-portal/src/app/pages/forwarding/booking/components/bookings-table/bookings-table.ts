import { DatePipe, DecimalPipe, UpperCasePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { BookingSummaryDto } from "@trackwise/shared/api";
import { truckTonnageClassOptions } from "@trackwise/shared/api/enums";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Card, UiDataTable, UiSortHeader } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";
import { formatTimeOfDay } from "@/shared/utils";
import { bookingStatusLabel } from "../../utils/booking-labels";

@Component({
  selector: "app-bookings-table",
  templateUrl: "./bookings-table.html",
  imports: [
    Card,
    DataContainer,
    DatePipe,
    DateFormatPipe,
    DecimalPipe,
    UpperCasePipe,
    RouterLink,
    UiDataTable,
    UiSortHeader,
  ],
})
export class BookingsTable {
  private readonly router = inject(Router);

  public readonly data = input.required<BookingSummaryDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(10);
  public readonly first = input(0);

  protected readonly statusLabel = bookingStatusLabel;
  protected readonly formatTimeOfDay = formatTimeOfDay;

  // protected truckSizeLabel(size: BookingSummaryDto["truckSize"]): string {
  //   return truckTonnageClassOptions.find((o) => o.value === size)?.label ?? "";
  // }

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected navigateToBooking(booking: BookingSummaryDto): void {
    if (booking.id) {
      this.router.navigateByUrl(`/bookings-executions/${booking.id}`);
    }
  }

  protected statusBadgeClass(status: BookingSummaryDto["status"]): string {
    switch (status) {
      case "confirmed":
      case "in_progress":
      case "resched":
        return "bg-blue-500/10 text-blue-600 dark:text-blue-400";
      case "completed":
        return "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400";
      case "cancelled":
      case "hold":
        return "bg-amber-500/10 text-amber-600 dark:text-amber-400";
      default:
        return "bg-muted text-muted-foreground";
    }
  }
}
