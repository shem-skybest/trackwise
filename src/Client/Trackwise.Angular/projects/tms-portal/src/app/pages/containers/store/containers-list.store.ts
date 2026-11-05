import { getContainers, type ContainerDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the containers list page.
 */
export const ContainersListStore = createListStore<ContainerDto>(getContainers, {
  defaultSortField: "-CreatedAt",
  defaultPageSize: 10,
});
