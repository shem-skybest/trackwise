import { getSubscriptionPlans, type SubscriptionPlanDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the subscription plans list page.
 */
export const PlansListStore = createListStore<SubscriptionPlanDto>(getSubscriptionPlans, {
  defaultSortField: "Name",
  defaultPageSize: 10,
});
