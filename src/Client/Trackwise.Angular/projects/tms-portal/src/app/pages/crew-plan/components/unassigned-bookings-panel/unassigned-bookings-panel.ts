import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  confirmBooking,
  getUnassignedBookings,
  type UnassignedBookingDto,
} from "@trackwise/shared/api";
import { truckTonnageClassOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { ToastService } from "@trackwise/shared/services";
import { Badge, UiButton, UiDataTable } from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { CreateTripDialog } from "@/shared/components/domain-forms";

/**
 * The day's Cold Chain bookings with no trip yet - what the dispatcher checks every evening while
 * planning the next day. Confirmed ones are waiting for a truck; pending ones still need confirming.
 */
@Component({
  selector: "app-unassigned-bookings-panel",
  templateUrl: "./unassigned-bookings-panel.html",
  imports: [Badge, CreateTripDialog, RouterLink, UiButton, UiDataTable],
})
export class UnassignedBookingsPanel {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  /** The plan's day, as an ISO date. */
  public readonly date = input.required<string>();
  /** Bumped by the page's Refresh. */
  public readonly reloadKey = input(0);
  /** How many bookings still have no trip, for the tab's label. */
  public readonly countChange = output<number>();

  protected readonly canManage =
    this.permissions.hasPermission(Permission.Booking.Manage) &&
    this.permissions.hasPermission(Permission.Load.Manage);

  protected readonly bookings = signal<UnassignedBookingDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly busyId = signal<string | null>(null);
  protected readonly tripFor = signal<UnassignedBookingDto | null>(null);
  protected readonly tripDialogVisible = signal(false);

  protected readonly rows = computed(() =>
    this.bookings().map((booking) => ({
      booking,
      callTime: booking.callTime?.slice(0, 5) ?? "",
      truckSize: truckTonnageClassOptions.find((o) => o.value === booking.truckSize)?.label ?? "",
      drops: (booking.dropNames ?? []).join(", "),
      isConfirmed: booking.status === "confirmed",
    })),
  );

  constructor() {
    effect(() => {
      this.reloadKey();
      void this.load(this.date());
    });
  }

  protected openTripDialog(booking: UnassignedBookingDto): void {
    this.tripFor.set(booking);
    this.tripDialogVisible.set(true);
  }

  protected onTripCreated(): void {
    void this.load(this.date());
  }

  protected async confirm(booking: UnassignedBookingDto): Promise<void> {
    if (this.busyId()) {
      return;
    }

    this.busyId.set(booking.id!);
    try {
      await this.api.invoke(confirmBooking, { id: booking.id!, body: { carrierReference: null } });
      this.toast.showSuccess(`${booking.referenceNumber ?? "Booking"} confirmed`);
      await this.load(this.date());
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't confirm the booking."));
    } finally {
      this.busyId.set(null);
    }
  }

  private async load(date: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const bookings = (await this.api.invoke(getUnassignedBookings, { Date: date })) ?? [];
      this.bookings.set(bookings);
      this.countChange.emit(bookings.length);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the day's bookings."));
    } finally {
      this.isLoading.set(false);
    }
  }
}
