import { Component, input } from "@angular/core";
import { CurrencyFormatPipe } from "@trackwise/shared";
import { type ExpenseStatsDto } from "@trackwise/shared/api";
import { Card, Grid, Typography } from "@trackwise/shared/ui";

@Component({
  selector: "app-expense-analytics-summary",
  templateUrl: "./expense-analytics-summary.html",
  imports: [Card, CurrencyFormatPipe, Grid, Typography],
})
export class ExpenseAnalyticsSummary {
  public readonly stats = input.required<ExpenseStatsDto>();
}
