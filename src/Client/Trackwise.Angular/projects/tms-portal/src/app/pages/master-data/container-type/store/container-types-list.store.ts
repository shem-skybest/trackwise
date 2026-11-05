import { getContainerTypes, type ContainerTypeDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const ContainerTypesListStore = createListStore<ContainerTypeDto>(getContainerTypes, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
