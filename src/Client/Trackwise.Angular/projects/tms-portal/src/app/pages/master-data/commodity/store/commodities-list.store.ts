import { getCommodities, type CommodityDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const CommoditiesListStore = createListStore<CommodityDto>(getCommodities, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});
