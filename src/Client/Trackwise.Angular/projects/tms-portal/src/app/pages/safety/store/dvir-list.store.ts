import { getDvirReports, type DvirReportDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the DVIR reports list page.
 */
export const DvirListStore = createListStore<DvirReportDto>(getDvirReports, {
  defaultSortField: "-InspectionDate",
  defaultPageSize: 10,
});
