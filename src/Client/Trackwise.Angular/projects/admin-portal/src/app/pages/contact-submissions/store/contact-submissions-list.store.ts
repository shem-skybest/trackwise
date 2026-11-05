import { getContactSubmissions, type ContactSubmissionDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the contact submissions list page.
 */
export const ContactSubmissionsListStore = createListStore<ContactSubmissionDto>(
  getContactSubmissions,
  {
    defaultSortField: "-CreatedAt",
    defaultPageSize: 10,
  },
);
