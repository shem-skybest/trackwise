import { CommonModule } from "@angular/common";
import { Component, computed, input } from "@angular/core";
import type { InvoiceLineItemDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Divider } from "@trackwise/shared/ui";

@Component({
  selector: "app-payroll-pay-summary",
  templateUrl: "./pay-summary.html",
  imports: [CommonModule, CurrencyFormatPipe, Divider],
})
export class PayrollPaySummary {
  public readonly lineItems = input<InvoiceLineItemDto[]>([]);

  protected readonly grossPay = computed(() => {
    return this.lineItems()
      .filter((item) => item.type !== "deduction")
      .reduce((sum, item) => sum + (item.total ?? 0), 0);
  });

  protected readonly totalDeductions = computed(() => {
    return this.lineItems()
      .filter((item) => item.type === "deduction")
      .reduce((sum, item) => sum + (item.total ?? 0), 0);
  });

  protected readonly netPay = computed(() => {
    return this.grossPay() - this.totalDeductions();
  });
}
