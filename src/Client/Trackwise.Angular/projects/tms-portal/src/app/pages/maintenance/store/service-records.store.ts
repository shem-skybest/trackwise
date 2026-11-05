import { getMaintenanceRecords, type MaintenanceRecordDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the maintenance service records list page.
 */
export const ServiceRecordsStore = createListStore<MaintenanceRecordDto>(getMaintenanceRecords, {
  defaultSortField: "-ServiceDate",
  defaultPageSize: 10,
});
