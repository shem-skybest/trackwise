import { DecimalPipe } from "@angular/common";
import { Component, input, output } from "@angular/core";
import { type LoadBoardListingDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe, DateFormatPipe, DistanceUnitPipe } from "@trackwise/shared/pipes";
import { Badge, UiButton, UiDataTable, UiTooltip } from "@trackwise/shared/ui";
import { getCreditSeverity, getProviderSeverity } from "../loadboard.constants";

@Component({
  selector: "app-loadboard-search-results",
  templateUrl: "./loadboard-search-results.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DateFormatPipe,
    DecimalPipe,
    DistanceUnitPipe,
    UiButton,
    UiDataTable,
    UiTooltip,
  ],
})
export class LoadBoardSearchResults {
  public readonly listings = input.required<LoadBoardListingDto[]>();
  public readonly book = output<LoadBoardListingDto>();

  protected readonly getProviderSeverity = getProviderSeverity;
  protected readonly getCreditSeverity = getCreditSeverity;
}
