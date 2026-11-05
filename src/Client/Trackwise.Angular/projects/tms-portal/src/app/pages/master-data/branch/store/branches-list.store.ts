import { getBranches, type BranchDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const BranchesListStore = createListStore<BranchDto>(getBranches, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
