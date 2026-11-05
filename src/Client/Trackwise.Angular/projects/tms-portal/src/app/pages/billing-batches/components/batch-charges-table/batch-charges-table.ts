import { Component, input, output } from "@angular/core";
import type { BillingBatchChargeDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Badge, UiButton, UiDataTable } from "@trackwise/shared/ui";
import { chargeKindLabel } from "../../utils/billing-batch-labels";

@Component({
  selector: "app-batch-charges-table",
  templateUrl: "./batch-charges-table.html",
  imports: [Badge, CurrencyFormatPipe, UiButton, UiDataTable],
})
export class BatchChargesTable {
  public readonly charges = input.required<BillingBatchChargeDto[]>();
  public readonly editable = input(false);
  public readonly remove = output<BillingBatchChargeDto>();

  protected readonly kindLabel = chargeKindLabel;
}
