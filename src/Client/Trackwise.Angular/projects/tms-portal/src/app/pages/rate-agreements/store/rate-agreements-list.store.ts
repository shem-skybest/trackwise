import { getRateAgreements, type RateAgreementDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const RateAgreementsListStore = createListStore<RateAgreementDto>(getRateAgreements, {
  defaultPageSize: 25,
});
