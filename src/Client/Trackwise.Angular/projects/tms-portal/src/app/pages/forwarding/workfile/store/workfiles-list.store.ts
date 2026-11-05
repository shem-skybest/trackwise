import { getWorkfiles, type WorkfileSummaryDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const WorkfilesListStore = createListStore<WorkfileSummaryDto>(getWorkfiles, {
  defaultSortField: "-CreatedAt",
  defaultPageSize: 10,
});
