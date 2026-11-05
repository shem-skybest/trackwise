import { getIncidents, type IncidentDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const IncidentsListStore = createListStore<IncidentDto>(getIncidents, {
  defaultPageSize: 25,
  defaultSortField: "-OccurredAt",
});
