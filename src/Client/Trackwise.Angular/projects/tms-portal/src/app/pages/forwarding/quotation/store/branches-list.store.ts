import { createListStore, getBranches, type BranchDto } from "@trackwise/shared";

export const BranchesStore = createListStore<BranchDto>(getBranches, {
  defaultSortField: "Name",
  defaultPageSize: 100,
});
