import { getDemoRequests, type DemoRequestDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the demo requests list page.
 */
export const DemoRequestsListStore = createListStore<DemoRequestDto>(getDemoRequests, {
  defaultSortField: "-CreatedAt",
  defaultPageSize: 10,
});
