import { getCashFunds, type CashFundDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const CashFundsListStore = createListStore<CashFundDto>(getCashFunds, {
  defaultPageSize: 25,
});
