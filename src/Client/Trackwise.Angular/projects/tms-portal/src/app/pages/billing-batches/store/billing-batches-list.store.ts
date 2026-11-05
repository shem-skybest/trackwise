import { getBillingBatches, type BillingBatchDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const BillingBatchesListStore = createListStore<BillingBatchDto>(getBillingBatches, {
  defaultPageSize: 25,
});
