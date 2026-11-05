import { getPorts, type TerminalDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const PortsListStore = createListStore<TerminalDto>(getPorts, {
  defaultSortField: "Code",
  defaultPageSize: 25,
});
