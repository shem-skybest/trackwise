import { getTenants, type TenantDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the tenants list page.
 */
export const TenantsListStore = createListStore<TenantDto>(getTenants, {
  defaultSortField: "Name",
  defaultPageSize: 10,
});
