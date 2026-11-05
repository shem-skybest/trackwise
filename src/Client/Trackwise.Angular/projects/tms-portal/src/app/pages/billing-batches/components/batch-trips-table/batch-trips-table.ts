import { DatePipe } from "@angular/common";
import { Component, input, output } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { BillingBatchTripDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Badge, UiButton, UiDataTable } from "@trackwise/shared/ui";
import { isoToDate } from "@/shared/utils";
import { tripOutcome } from "../../utils/billing-batch-labels";

/** The trips on a batch with what each is charged; a trip that can't be billed yet says why. */
@Component({
  selector: "app-batch-trips-table",
  templateUrl: "./batch-trips-table.html",
  imports: [Badge, CurrencyFormatPipe, DatePipe, RouterLink, UiButton, UiDataTable],
})
export class BatchTripsTable {
  public readonly trips = input.required<BillingBatchTripDto[]>();
  public readonly editable = input(false);
  public readonly remove = output<BillingBatchTripDto>();

  protected readonly outcome = tripOutcome;
  protected readonly isoToDate = isoToDate;
}
