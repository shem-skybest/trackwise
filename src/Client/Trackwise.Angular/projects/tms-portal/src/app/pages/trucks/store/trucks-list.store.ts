import { getTrucksMasterData, type TruckDto as TruckMasterDataDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the trucks list page. Reads through `/master-data/trucks` (not the old `/trucks`
 * dispatch surface) because this is the steward page now, and only the master-data list supports
 * `IncludeDeleted` - needed for the restore action.
 */
export const TrucksListStore = createListStore<TruckMasterDataDto>(getTrucksMasterData, {
  defaultSortField: "Number",
  defaultPageSize: 10,
});
