import { DecimalPipe, TitleCasePipe } from "@angular/common";
import { Component, input, output } from "@angular/core";
import type { QuotationLineDto } from "@trackwise/shared/api";
import { Card, EmptyState, UiButton, UiDataTable } from "@trackwise/shared/ui";

@Component({
  selector: "app-quotation-lines-table",
  templateUrl: "./quotation-lines-table.html",
  imports: [Card, DecimalPipe, EmptyState, TitleCasePipe, UiButton, UiDataTable],
})
export class QuotationLinesTable {
  public readonly lines = input.required<QuotationLineDto[]>();
  public readonly remove = output<string>();

  protected formatText(val?: string | null): string {
    if (!val) return "-";
    return val.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
  }

  protected transform(
    quantity: number | string | null | undefined,
    unit: string | null | undefined,
  ): string {
    if (quantity == null && !unit) return "";
    if (quantity == null) return unit ?? "";
    if (!unit) return `${quantity}`;

    const num = Number(quantity);
    let cleanedUnit = unit.trim();

    // 1. Remove leading "per " or "per" (e.g., "per trip" -> "trip")
    cleanedUnit = cleanedUnit.replace(/^per\s+/i, "");

    // 2. Pluralize basic units if quantity is not 1 (e.g., 2 trip -> 2 trips)
    if (!isNaN(num) && num !== 1) {
      cleanedUnit = this.pluralize(cleanedUnit);
    }

    // 3. Prevent space before % (e.g., "1 % of linehaul" -> "1% of linehaul")
    if (cleanedUnit.startsWith("%")) {
      return `${quantity}${cleanedUnit}`;
    }

    return `${quantity} (${cleanedUnit})`;
  }

  private pluralize(unit: string): string {
    // Only pluralize simple words (avoid touching expressions like "% of linehaul")
    const words = unit.split(" ");
    const firstWord = words[0].toLowerCase();

    const rules: Record<string, string> = {
      trip: "trips",
      hour: "hours",
      day: "days",
      mile: "miles",
      stop: "stops",
      load: "loads",
    };

    if (rules[firstWord]) {
      words[0] = rules[firstWord];
      return words.join(" ");
    }

    return unit;
  }
}
