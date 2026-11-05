import { DatePipe, DecimalPipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { FuelFillUpDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Badge, UiButton, UiDataTable, UiSortHeader, UiTooltip } from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";
import { FuelFillUpsStore } from "../../store/fuel-fill-ups.store";
import { cardLabel, reviewIntent, reviewLabel } from "../../utils/fuel-labels";

/** The fill-ups on the page's store, latest first, with the review actions the user may take. */
@Component({
  selector: "app-fuel-fill-ups-table",
  templateUrl: "./fuel-fill-ups-table.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DataContainer,
    DatePipe,
    DecimalPipe,
    RouterLink,
    UiTooltip,
    UiButton,
    UiDataTable,
    UiSortHeader,
  ],
})
export class FuelFillUpsTable {
  protected readonly store = inject(FuelFillUpsStore);

  public readonly canManage = input(false);
  public readonly canReview = input(false);
  public readonly busy = input(false);

  public readonly edit = output<FuelFillUpDto>();
  public readonly verify = output<FuelFillUpDto>();
  public readonly flag = output<FuelFillUpDto>();
  public readonly remove = output<FuelFillUpDto>();
  public readonly receipt = output<FuelFillUpDto>();

  protected readonly cardLabel = cardLabel;
  protected readonly reviewLabel = reviewLabel;
  protected readonly reviewIntent = reviewIntent;
}
