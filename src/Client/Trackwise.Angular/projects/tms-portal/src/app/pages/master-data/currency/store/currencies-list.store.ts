import { getCurrencies, type CurrencyDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const CurrenciesListStore = createListStore<CurrencyDto>(getCurrencies, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
