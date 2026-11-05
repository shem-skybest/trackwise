import { getDriverBehaviorEvents, type DriverBehaviorEventDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the driver behavior events list page.
 */
export const DriverBehaviorListStore = createListStore<DriverBehaviorEventDto>(
  getDriverBehaviorEvents,
  {
    defaultSortField: "-OccurredAt",
    defaultPageSize: 25,
  },
);
