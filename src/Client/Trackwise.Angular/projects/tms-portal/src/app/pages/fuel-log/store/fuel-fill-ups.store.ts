import { getFuelFillUps, type FuelFillUpDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const FuelFillUpsStore = createListStore<FuelFillUpDto>(getFuelFillUps, {
  defaultPageSize: 50,
});
