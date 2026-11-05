import { getUsers, type UserDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the users list page.
 */
export const UsersListStore = createListStore<UserDto>(getUsers, {
  defaultSortField: "Email",
  defaultPageSize: 10,
});
