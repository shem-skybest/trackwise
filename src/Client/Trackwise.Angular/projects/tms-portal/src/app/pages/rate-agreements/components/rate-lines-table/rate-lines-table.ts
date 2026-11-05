import { CurrencyPipe, DecimalPipe } from "@angular/common";
import { Component, computed, input } from "@angular/core";
import type { RateAgreementDto, RateAgreementLineDto } from "@trackwise/shared/api";
import { UiDataTable } from "@trackwise/shared/ui";
import { tonnageLabel } from "../../utils/rate-agreement-labels";

/** An agreement's rate card, laid out like the Monday board it replaces. */
@Component({
  selector: "app-rate-lines-table",
  templateUrl: "./rate-lines-table.html",
  imports: [CurrencyPipe, DecimalPipe, UiDataTable],
})
export class RateLinesTable {
  public readonly agreement = input.required<RateAgreementDto>();

  protected readonly tonnageLabel = tonnageLabel;

  protected readonly lines = computed(() => this.agreement().lines ?? []);

  protected readonly isBandCard = computed(() => this.lines().some((l) => l.minKm != null));

  /** The diesel prices the card has surcharges for - one column each, as on Monday. */
  protected readonly tiers = computed(() => {
    if (this.agreement().fuelMethod !== "diesel_tier_surcharge") {
      return [];
    }
    const prices = new Set<number>();
    for (const line of this.lines()) {
      for (const s of line.fuelSurcharges ?? []) {
        prices.add(s.dieselPrice ?? 0);
      }
    }
    return [...prices].sort((a, b) => a - b);
  });

  protected readonly showPercent = computed(() => this.agreement().fuelMethod === "percentage");

  protected surcharge(line: RateAgreementLineDto, tier: number): number | null {
    return line.fuelSurcharges?.find((s) => s.dieselPrice === tier)?.amount ?? null;
  }

  protected place(line: RateAgreementLineDto): string {
    if (line.minKm != null) {
      return `${line.minKm}-${line.maxKm} km`;
    }
    return [line.city, line.province].filter(Boolean).join(", ") || "-";
  }
}
