import { getCustomers, type CustomerDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the customers list page.
 */
export const CustomersListStore = createListStore<CustomerDto>(getCustomers, {
  defaultSortField: "Name",
  defaultPageSize: 10,
});
