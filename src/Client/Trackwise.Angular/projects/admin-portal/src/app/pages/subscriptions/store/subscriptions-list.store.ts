import { getSubscriptions, type SubscriptionDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the subscriptions list page.
 */
export const SubscriptionsListStore = createListStore<SubscriptionDto>(getSubscriptions, {
  defaultSortField: "-Status",
  defaultPageSize: 10,
});
