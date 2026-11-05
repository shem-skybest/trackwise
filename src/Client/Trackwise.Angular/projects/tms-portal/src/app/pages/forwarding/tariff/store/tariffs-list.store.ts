import { getTariffs, type TariffDto } from "@trackwise/shared/api/generated";
import { createListStore } from "@trackwise/shared/stores";

export const TariffsListStore = createListStore<TariffDto>(getTariffs, {
  defaultSortField: "Name",
  defaultPageSize: 10,
});
