import { DatePipe } from "@angular/common";
import { Component, input } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { InvoiceDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Icon, UiButton, UiDataTable, UiTooltip } from "@trackwise/shared/ui";
import { InvoiceStatusTag } from "@/shared/components";

@Component({
  selector: "app-customer-invoices-list",
  templateUrl: "./customer-invoices-list.html",
  imports: [
    CurrencyFormatPipe,
    DatePipe,
    Icon,
    InvoiceStatusTag,
    RouterLink,
    UiButton,
    UiDataTable,
    UiTooltip,
  ],
})
export class CustomerInvoicesList {
  public readonly invoices = input<InvoiceDto[]>([]);
  public readonly customerId = input<string>();
  public readonly isLoading = input(false);
}
