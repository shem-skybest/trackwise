import { getTrips, type TripDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the trips list page.
 */
export const TripsListStore = createListStore<TripDto>(getTrips, {
  defaultSortField: "-CreatedAt",
  defaultPageSize: 10,
});
