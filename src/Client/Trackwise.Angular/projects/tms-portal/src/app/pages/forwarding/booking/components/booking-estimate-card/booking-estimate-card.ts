import { CurrencyPipe } from "@angular/common";
import { Component, computed, input } from "@angular/core";
import type { BookingEstimateDto } from "@trackwise/shared/api";
import { Alert, Card, Spinner, Stack, Typography } from "@trackwise/shared/ui";

/**
 * A Cold Chain booking's price broken down the way the rate card and the bill read it: the rate of
 * the costliest drop, fuel on top, and extra drops.
 */
@Component({
  selector: "app-booking-estimate-card",
  templateUrl: "./booking-estimate-card.html",
  imports: [Alert, Card, CurrencyPipe, Spinner, Stack, Typography],
})
export class BookingEstimateCard {
  public readonly estimate = input<BookingEstimateDto | null>(null);
  public readonly isLoading = input(false);
  public readonly title = input("Price from agreement");

  protected readonly currency = computed(() => this.estimate()?.currency ?? "PHP");

  /** Shown when there is nothing to price yet. */
  public readonly emptyText = input(
    "Pick the client, day, truck size and an outlet to see the price.",
  );
}
