import { DecimalPipe } from "@angular/common";
import { Component, input } from "@angular/core";
import type { FuelSummaryDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Card, Grid, Typography } from "@trackwise/shared/ui";
import { cardLabel } from "../../utils/fuel-labels";

/** The period's totals, and each fleet card's, to check against that card's monthly statement. */
@Component({
  selector: "app-fuel-summary",
  templateUrl: "./fuel-summary.html",
  imports: [Card, CurrencyFormatPipe, DecimalPipe, Grid, Typography],
})
export class FuelSummary {
  public readonly summary = input.required<FuelSummaryDto>();

  protected readonly cardLabel = cardLabel;
}
