import { getTrailers, type TrailerDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const TrailersListStore = createListStore<TrailerDto>(getTrailers, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
