import { getTimeEntries, type TimeEntryDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the timesheets list page.
 */
export const TimesheetsListStore = createListStore<TimeEntryDto>(getTimeEntries, {
  defaultSortField: "-Date",
  defaultPageSize: 25,
});
