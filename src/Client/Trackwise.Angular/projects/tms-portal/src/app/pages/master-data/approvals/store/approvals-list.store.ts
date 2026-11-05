import { getMasterDataApprovals, type MasterDataApprovalDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * The queue is ordered oldest-first by the server - what has been waiting longest is what an
 * approver should see first - so there is no default sort field here to override it.
 */
export const ApprovalsListStore = createListStore<MasterDataApprovalDto>(getMasterDataApprovals, {
  defaultPageSize: 25,
});
