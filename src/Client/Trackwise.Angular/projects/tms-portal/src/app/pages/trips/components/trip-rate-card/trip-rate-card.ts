import { CurrencyPipe, DatePipe } from "@angular/common";
import { Component, computed, effect, inject, signal, untracked } from "@angular/core";
import { RouterLink } from "@angular/router";
import { Api, getTripRateQuote, priceTrip, type TripRateQuoteDto } from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import {
  Alert,
  Card,
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { dateToIso, isoToDate } from "@/shared/utils";
import { TripDetailsStore } from "../../store/trip-details.store";

/**
 * The truck-trip price from the client's rate agreement: what was applied, and a preview before
 * (re)applying it. Applying writes the charge onto the trip's drops, so trip revenue follows.
 */
@Component({
  selector: "app-trip-rate-card",
  templateUrl: "./trip-rate-card.html",
  imports: [
    Alert,
    Card,
    CurrencyPipe,
    DatePipe,
    Icon,
    RouterLink,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
  ],
})
export class TripRateCard {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);
  protected readonly store = inject(TripDetailsStore);

  protected readonly canPrice = this.permissions.hasPermission(Permission.Load.Manage);
  protected readonly trip = computed(() => this.store.trip());
  protected readonly isPriced = computed(() => this.trip()?.rateTotal != null);

  protected readonly dialogOpen = signal(false);
  protected readonly ratedOn = signal<Date | null>(null);
  protected readonly quote = signal<TripRateQuoteDto | null>(null);
  protected readonly quoteError = signal<string | null>(null);
  protected readonly isQuoting = signal(false);
  protected readonly isApplying = signal(false);

  protected readonly isoToDate = isoToDate;

  constructor() {
    // Re-quote whenever the date changes while the dialog is open.
    effect(() => {
      const date = this.ratedOn();
      if (this.dialogOpen() && date) {
        untracked(() => void this.preview(date));
      }
    });
  }

  protected open(): void {
    const trip = this.trip();
    this.ratedOn.set(isoToDate(trip?.ratedOn) ?? new Date());
    this.quote.set(null);
    this.quoteError.set(null);
    this.dialogOpen.set(true);
  }

  protected async apply(): Promise<void> {
    const trip = this.trip();
    if (!trip?.id) {
      return;
    }

    this.isApplying.set(true);
    try {
      await this.api.invoke(priceTrip, {
        tripId: trip.id,
        body: { ratedOn: dateToIso(this.ratedOn()) },
      });
      this.toast.showSuccess("Trip priced from the rate agreement");
      this.dialogOpen.set(false);
      await this.store.refreshTrip();
    } catch (error) {
      this.toast.showError(messageOf(error, "Could not price the trip."));
    } finally {
      this.isApplying.set(false);
    }
  }

  private async preview(date: Date): Promise<void> {
    const trip = this.trip();
    if (!trip?.id) {
      return;
    }

    this.isQuoting.set(true);
    this.quoteError.set(null);
    try {
      this.quote.set(
        await this.api.invoke(getTripRateQuote, {
          tripId: trip.id,
          ratedOn: dateToIso(date) ?? undefined,
        }),
      );
    } catch (error) {
      this.quote.set(null);
      this.quoteError.set(messageOf(error, "Could not work out a price."));
    } finally {
      this.isQuoting.set(false);
    }
  }
}

function messageOf(error: unknown, fallback: string): string {
  return (error as { error?: { error?: string } })?.error?.error ?? fallback;
}
