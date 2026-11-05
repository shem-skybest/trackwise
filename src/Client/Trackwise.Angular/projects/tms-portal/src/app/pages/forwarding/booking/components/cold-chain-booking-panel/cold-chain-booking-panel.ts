// import { DatePipe } from "@angular/common";
// import { Component, computed, input } from "@angular/core";
// import { RouterLink } from "@angular/router";
// import type { BookingDropDto, BookingDto, BookingEstimateDto } from "@trackwise/shared/api";
// import {
//   bookingLossReasonOptions,
//   cancellationPartyOptions,
//   truckTonnageClassOptions,
// } from "@trackwise/shared/api/enums";
// import { Alert, Card, Grid, Stack, Typography } from "@trackwise/shared/ui";
// import { formatTimeOfDay } from "@/shared/utils";
// import { BookingEstimateCard } from "../booking-estimate-card/booking-estimate-card";

// /** A Cold Chain booking's trip plan, drops, estimate and the trip that runs it. */
// @Component({
//   selector: "app-cold-chain-booking-panel",
//   templateUrl: "./cold-chain-booking-panel.html",
//   imports: [Alert, BookingEstimateCard, Card, DatePipe, Grid, RouterLink, Stack, Typography],
// })
// export class ColdChainBookingPanel {
//   public readonly booking = input.required<BookingDto>();

//   protected readonly formatTimeOfDay = formatTimeOfDay;

//   protected addressOf(drop: BookingDropDto): string {
//     return [drop.streetAddress, drop.city].filter((part) => part?.trim()).join(", ");
//   }

//   protected readonly truckSize = computed(
//     () => truckTonnageClassOptions.find((o) => o.value === this.booking().truckSize)?.label ?? "-",
//   );

//   protected readonly temperature = computed(() => {
//     const { temperatureMinC: low, temperatureMaxC: high } = this.booking();
//     if (low == null && high == null) {
//       return "-";
//     }
//     return low != null && high != null ? `${low} to ${high} °C` : `${low ?? high} °C`;
//   });

//   protected readonly totalBoxes = computed(() =>
//     (this.booking().drops ?? []).reduce((sum, d) => sum + (d.plannedBoxes ?? 0), 0),
//   );

//   protected readonly totalBags = computed(() =>
//     (this.booking().drops ?? []).reduce((sum, d) => sum + (d.plannedBags ?? 0), 0),
//   );

//   protected readonly lossSummary = computed(() => {
//     const b = this.booking();
//     if (!b.lossReason) {
//       return null;
//     }
//     const reason = bookingLossReasonOptions.find((o) => o.value === b.lossReason)?.label;
//     const party = cancellationPartyOptions.find((o) => o.value === b.cancelledByParty)?.label;
//     return [party, reason].filter(Boolean).join(" · ");
//   });

//   /** The estimate saved with the booking, in the shape the estimate card reads. */
//   protected readonly estimate = computed<BookingEstimateDto>(() => {
//     const b = this.booking();
//     const priced = b.estimatedTotal != null;
//     return {
//       agreementId: b.rateAgreementId,
//       agreementName: b.rateAgreementName,
//       lineLabel: null,
//       baseAmount: b.estimatedBaseAmount,
//       fuelAmount: b.estimatedFuelAmount,
//       fuelBasis: null,
//       extraDrops: Math.max((b.drops?.length ?? 1) - 1, 0),
//       extraDropAmount: b.estimatedExtraDropAmount,
//       total: b.estimatedTotal,
//       currency: b.currency ?? "PHP",
//       warnings: priced && b.estimateNote ? [b.estimateNote] : [],
//       error: priced ? null : (b.estimateNote ?? "Not priced yet."),
//     };
//   });
// }
