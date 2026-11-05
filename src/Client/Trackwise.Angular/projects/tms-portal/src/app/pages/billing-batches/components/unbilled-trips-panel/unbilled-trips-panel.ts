import { DatePipe } from "@angular/common";
import { Component, inject, input, output, signal } from "@angular/core";
import type { AppError } from "@trackwise/shared";
import {
  Api,
  getUnbilledTrips,
  type BillingBatchTripDto,
  type CustomerDto,
  type UnbilledCustomerDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Badge, Card, Stack, Typography, UiButton } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";
import { isoToDate } from "@/shared/utils";
import { billingCycleLabel } from "../../utils/billing-batch-labels";
import type { BillingBatchPrefill } from "../billing-batch-dialog/billing-batch-dialog";

/**
 * What's left to bill: per client, the completed trips no batch has taken yet, with the period the
 * oldest falls in. A per-trip client's trips are billed one at a time.
 */
@Component({
  selector: "app-unbilled-trips-panel",
  templateUrl: "./unbilled-trips-panel.html",
  imports: [Badge, Card, CurrencyFormatPipe, DataContainer, DatePipe, Stack, Typography, UiButton],
})
export class UnbilledTripsPanel {
  private readonly api = inject(Api);

  public readonly canManage = input(false);
  public readonly bill = output<BillingBatchPrefill>();

  protected readonly clients = signal<UnbilledCustomerDto[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly error = signal<AppError | null>(null);

  protected readonly cycleLabel = billingCycleLabel;
  protected readonly isoToDate = isoToDate;

  constructor() {
    void this.reload();
  }

  public async reload(): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      this.clients.set(await this.api.invoke(getUnbilledTrips, {}));
    } catch (originalError) {
      this.error.set({
        category: "server",
        message: "Could not load the trips left to bill.",
        retryable: true,
        originalError,
      });
    } finally {
      this.isLoading.set(false);
    }
  }

  protected billPeriod(client: UnbilledCustomerDto): void {
    const start = isoToDate(client.suggestedPeriodStart ?? client.oldestDate);
    const end = isoToDate(client.suggestedPeriodEnd ?? client.oldestDate);
    if (start && end) {
      this.bill.emit({ customer: this.customerOf(client), periodStart: start, periodEnd: end });
    }
  }

  protected billTrip(client: UnbilledCustomerDto, trip: BillingBatchTripDto): void {
    const day = isoToDate(trip.billingDate);
    if (day) {
      this.bill.emit({
        customer: this.customerOf(client),
        periodStart: day,
        periodEnd: day,
        tripIds: [trip.tripId!],
        tripLabel: `trip ${trip.tripNumber}`,
      });
    }
  }

  private customerOf(client: UnbilledCustomerDto): CustomerDto {
    return {
      id: client.customerId,
      name: client.customerName,
      billingCycle: client.billingCycle,
    } as CustomerDto;
  }
}
