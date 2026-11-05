import type { HttpClient, HttpContext } from "@angular/common/http";
import {
  getInvoices,
  type GetInvoices$Params,
  type InvoiceDto,
  type InvoiceType,
} from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export type BillingQueryParams = GetInvoices$Params & Record<string, unknown>;

export const BillingListStore = createListStore<InvoiceDto, BillingQueryParams>(
  (
    http: HttpClient,
    rootUrl: string,
    params: GetInvoices$Params | undefined,
    context: HttpContext | undefined,
  ) =>
    getInvoices(
      http,
      rootUrl,
      {
        ...params,
        InvoiceType: "booking" as InvoiceType,
      },
      context,
    ),
  {
    defaultSortField: "-Number",
    defaultPageSize: 10,
  },
);
