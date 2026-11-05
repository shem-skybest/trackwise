import { getQuotations, type QuotationSummaryDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const QuotationsListStore = createListStore<QuotationSummaryDto>(getQuotations, {
  defaultSortField: "CreatedAt",
  defaultPageSize: 10,
});
