import { getIftaTaxRates, type IftaTaxRateDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the IFTA tax rates list page.
 */
export const IftaRatesListStore = createListStore<IftaTaxRateDto>(getIftaTaxRates, {
  defaultSortField: "Jurisdiction.CountryCode",
  defaultPageSize: 25,
});
