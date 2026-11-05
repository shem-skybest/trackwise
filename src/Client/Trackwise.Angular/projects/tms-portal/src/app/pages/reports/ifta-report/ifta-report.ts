import { DecimalPipe, SlicePipe } from "@angular/common";
import { Component, computed, inject, signal, type OnInit } from "@angular/core";
import { jurisdictionLabel } from "@trackwise/shared";
import { Api, exportIftaReportPdf, getIftaReport, type IftaReportDto } from "@trackwise/shared/api";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import {
  Alert,
  Badge,
  DashboardCard,
  EmptyState,
  Spinner,
  Stack,
  UiButton,
  UiDataTable,
  UiSelectField,
  UiSortHeader,
  UiTooltip,
} from "@trackwise/shared/ui";
import { downloadBlobFile } from "@trackwise/shared/utils";
import { ToastService } from "@/core/services";
import { PageHeader, StatCard } from "@/shared/components";

interface QuarterOption {
  label: string;
  value: string; // "2026-3"
  year: number;
  quarter: number;
}

@Component({
  selector: "app-ifta-report",
  templateUrl: "./ifta-report.html",
  imports: [
    Alert,
    Badge,
    DashboardCard,
    DateFormatPipe,
    DecimalPipe,
    EmptyState,
    PageHeader,
    SlicePipe,
    Spinner,
    Stack,
    StatCard,
    UiButton,
    UiDataTable,
    UiSelectField,
    UiSortHeader,
    UiTooltip,
  ],
})
export class IftaReportComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  protected readonly isLoading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly report = signal<IftaReportDto | null>(null);

  protected readonly quarterOptions: QuarterOption[] = buildQuarterOptions();
  protected readonly selectedQuarter = signal<string>(this.quarterOptions[0].value);

  protected readonly hasData = computed(() => {
    const r = this.report();
    return !!r && ((r.jurisdictions?.length ?? 0) > 0 || r.totalMiles! > 0);
  });

  /** A net credit (negative tax due) is money back - show it as success; anything owed stays a warning. */
  protected readonly netTaxColor = computed<"green" | "orange">(() =>
    (this.report()?.totalTaxDue ?? 0) < 0 ? "green" : "orange",
  );

  /**
   * Filing deadline for the selected quarter, shown only once the quarter is over (fileable).
   * The current in-progress quarter and any future quarter get no banner.
   */
  protected readonly filingInfo = computed(() => {
    const opt = this.currentOption();
    const now = new Date();
    const curYear = now.getUTCFullYear();
    const curQuarter = Math.floor(now.getUTCMonth() / 3) + 1;
    const isCurrentOrFuture =
      opt.year > curYear || (opt.year === curYear && opt.quarter >= curQuarter);
    if (isCurrentOrFuture) {
      return null;
    }

    const deadline = iftaFilingDeadline(opt.year, opt.quarter);
    const days = Math.ceil((deadline.getTime() - now.getTime()) / 86_400_000);
    return { deadline, days, overdue: days < 0 };
  });

  /** Jurisdiction rows with the display label precomputed, so the table doesn't format per change-detection tick. */
  protected readonly jurisdictionRows = computed(() =>
    (this.report()?.jurisdictions ?? []).map((row) => ({
      ...row,
      label: jurisdictionLabel(row.countryCode, row.region),
    })),
  );

  ngOnInit(): void {
    void this.fetch();
  }

  protected onQuarterChange(value: string | null): void {
    if (value) {
      this.selectedQuarter.set(value);
      void this.fetch();
    }
  }

  protected async fetch(): Promise<void> {
    const option = this.currentOption();
    this.isLoading.set(true);
    try {
      const data = await this.api.invoke(getIftaReport, {
        Year: option.year,
        Quarter: option.quarter,
      });
      this.report.set(data ?? null);
    } catch (error) {
      console.error("IFTA report fetch failed:", error);
      this.toast.showError("Failed to load the IFTA report. Please try again.");
      this.report.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async exportPdf(): Promise<void> {
    const option = this.currentOption();
    this.exporting.set(true);
    try {
      const blob = await this.api.invoke(exportIftaReportPdf, {
        Year: option.year,
        Quarter: option.quarter,
      });
      downloadBlobFile(blob, `ifta-report-${option.year}-q${option.quarter}.pdf`);
    } catch (error) {
      console.error("IFTA PDF export failed:", error);
      this.toast.showError("Failed to export the IFTA report PDF");
    } finally {
      this.exporting.set(false);
    }
  }

  protected exportCsv(): void {
    const report = this.report();
    const option = this.currentOption();
    if (!report?.jurisdictions?.length) {
      this.toast.showWarning("No IFTA data to export");
      return;
    }

    const headers = [
      "Jurisdiction",
      "Miles",
      "Taxable Gallons",
      "Purchased Gallons",
      "Net Taxable Gallons",
      "Rate Per Gallon",
      "Surcharge Per Gallon",
      "Tax Due",
    ];
    const rows = report.jurisdictions.map((row) => [
      jurisdictionLabel(row.countryCode, row.region),
      row.miles ?? "",
      row.taxableGallons ?? "",
      row.purchasedGallons ?? "",
      row.netTaxableGallons ?? "",
      row.rateMissing ? "missing" : (row.ratePerGallon ?? ""),
      row.surchargeRatePerGallon ?? "",
      row.taxDue ?? "",
    ]);

    const csvContent = [headers, ...rows]
      .map((row) => row.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(","))
      .join("\n");

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    downloadBlobFile(blob, `ifta-report-${option.year}-q${option.quarter}.csv`);
    this.toast.showSuccess(`Exported ${report.jurisdictions.length} jurisdiction(s) to CSV`);
  }

  private currentOption(): QuarterOption {
    return (
      this.quarterOptions.find((o) => o.value === this.selectedQuarter()) ?? this.quarterOptions[0]
    );
  }
}

/**
 * IFTA filing deadline: the last day of the month following quarter-end
 * (Q1 -> Apr 30, Q2 -> Jul 31, Q3 -> Oct 31, Q4 -> Jan 31 of the next year).
 * Mirrors the backend `IftaQuarters.FilingDeadline`. Day 0 of the next month is the last
 * day of the target month.
 */
function iftaFilingDeadline(year: number, quarter: number): Date {
  const target: Record<number, { y: number; m: number }> = {
    1: { y: year, m: 4 }, // May(4) day 0 = Apr 30
    2: { y: year, m: 7 }, // Aug(7) day 0 = Jul 31
    3: { y: year, m: 10 }, // Nov(10) day 0 = Oct 31
    4: { y: year + 1, m: 1 }, // Feb(1) day 0 = Jan 31
  };
  const { y, m } = target[quarter] ?? target[4];
  return new Date(Date.UTC(y, m, 0));
}

/** Current quarter first, then the 7 before it. */
function buildQuarterOptions(): QuarterOption[] {
  const options: QuarterOption[] = [];
  const now = new Date();
  let year = now.getUTCFullYear();
  let quarter = Math.floor(now.getUTCMonth() / 3) + 1;

  for (let i = 0; i < 8; i++) {
    options.push({
      label: `Q${quarter} ${year}${i === 0 ? " (current)" : ""}`,
      value: `${year}-${quarter}`,
      year,
      quarter,
    });
    quarter--;
    if (quarter === 0) {
      quarter = 4;
      year--;
    }
  }

  return options;
}
