import { Component, computed, input, output, signal } from "@angular/core";
import { Stack, Typography, UiButton, UiNumberField } from "@trackwise/shared/ui";

interface ProvinceRow {
  province: string;
  rows: number;
  /** The rows' shared percentage, or null when they differ. */
  percent: number | null;
}

/**
 * Sets the fuel percentage of every row in a province at once. Bayview re-issues its card when
 * diesel moves, with one percentage per area (Mega Manila, Bulacan, Cavite...), so a revision
 * changes a handful of numbers rather than ninety rows.
 */
@Component({
  selector: "app-province-fuel-percent",
  templateUrl: "./province-fuel-percent.html",
  imports: [Stack, Typography, UiButton, UiNumberField],
})
export class ProvinceFuelPercent {
  public readonly lines =
    input.required<readonly { province: string; fuelPercent: number | null }[]>();
  public readonly apply = output<{ province: string; percent: number }>();

  protected readonly drafts = signal<Record<string, number | null>>({});

  protected readonly provinces = computed<ProvinceRow[]>(() => {
    const byProvince = new Map<string, (number | null)[]>();
    for (const line of this.lines()) {
      const province = line.province.trim();
      if (province) {
        byProvince.set(province, [...(byProvince.get(province) ?? []), line.fuelPercent]);
      }
    }

    return [...byProvince.entries()]
      .map(([province, percents]) => ({
        province,
        rows: percents.length,
        percent: percents.every((p) => p === percents[0]) ? percents[0] : null,
      }))
      .sort((a, b) => a.province.localeCompare(b.province));
  });

  protected draftFor(row: ProvinceRow): number | null {
    return this.drafts()[row.province] ?? row.percent;
  }

  protected setDraft(province: string, value: number | null): void {
    this.drafts.update((d) => ({ ...d, [province]: value }));
  }

  protected applyRow(row: ProvinceRow): void {
    const percent = this.draftFor(row);
    if (percent != null) {
      this.apply.emit({ province: row.province, percent });
    }
  }
}
