import { DecimalPipe } from "@angular/common";
import { Component, input } from "@angular/core";
import type { FuelTruckTotalDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { UiDataTable } from "@trackwise/shared/ui";

/** Each truck's fuel for the period, and how far it went on it where odometer readings allow. */
@Component({
  selector: "app-fuel-trucks-table",
  templateUrl: "./fuel-trucks-table.html",
  imports: [CurrencyFormatPipe, DecimalPipe, UiDataTable],
})
export class FuelTrucksTable {
  public readonly trucks = input.required<FuelTruckTotalDto[]>();
}
