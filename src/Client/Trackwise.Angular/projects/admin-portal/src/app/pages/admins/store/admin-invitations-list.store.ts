import { getAdminInvitations, type InvitationDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the pending admin invitations list.
 */
export const AdminInvitationsListStore = createListStore<InvitationDto>(getAdminInvitations, {
  defaultPageSize: 10,
});
