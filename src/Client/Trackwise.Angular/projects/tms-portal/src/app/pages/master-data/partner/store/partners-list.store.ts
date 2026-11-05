import { getPartners, type BusinessPartnerDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const PartnersListStore = createListStore<BusinessPartnerDto>(getPartners, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});
