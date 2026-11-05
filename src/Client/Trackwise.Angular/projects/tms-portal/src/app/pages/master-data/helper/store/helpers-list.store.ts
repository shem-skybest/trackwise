import { getHelpers, type HelperDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const HelpersListStore = createListStore<HelperDto>(getHelpers, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
