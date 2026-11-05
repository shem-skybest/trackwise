import {
  getEmployeesMasterData,
  type EmployeeDto as EmployeeMasterDataDto,
} from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the employees list page. Reads through `/master-data/employees` (not the old
 * `/employees` dispatch/self-service surface) because this is the steward page now, and only the
 * master-data list supports `IncludeDeleted` - needed for the restore action.
 */
export const EmployeesListStore = createListStore<EmployeeMasterDataDto>(getEmployeesMasterData, {
  defaultSortField: "FirstName",
  defaultPageSize: 10,
});
