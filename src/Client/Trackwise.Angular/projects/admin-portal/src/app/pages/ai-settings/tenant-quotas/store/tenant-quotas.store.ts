import { getTenantQuotaUsages, type TenantQuotaUsageDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the tenant AI quota usage list page.
 * The sort key must match GetTenantQuotaUsagesHandler's default - there is no compiler link.
 */
export const TenantQuotasStore = createListStore<TenantQuotaUsageDto>(getTenantQuotaUsages, {
  defaultSortField: "-SpentThisWeekUsd",
});
