import { getLocations, type LocationDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const LocationsListStore = createListStore<LocationDto>(getLocations, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});
