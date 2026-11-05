import { getLoads, type LoadDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the loads list page.
 */
export const LoadsListStore = createListStore<LoadDto>(getLoads, {
  defaultSortField: "-DispatchedAt",
  defaultPageSize: 10,
});
