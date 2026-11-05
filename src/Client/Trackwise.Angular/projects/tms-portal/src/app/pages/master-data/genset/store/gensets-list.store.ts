import { getGensets, type GensetDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const GensetsListStore = createListStore<GensetDto>(getGensets, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
