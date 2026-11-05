import { getVessels, type VesselDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const VesselsListStore = createListStore<VesselDto>(getVessels, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});
