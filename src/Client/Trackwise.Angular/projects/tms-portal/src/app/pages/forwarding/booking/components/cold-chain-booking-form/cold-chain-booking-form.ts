// import {
//   Component,
//   computed,
//   DestroyRef,
//   effect,
//   inject,
//   input,
//   output,
//   signal,
//   untracked,
// } from "@angular/core";
// import {
//   applyEach,
//   disabled,
//   form,
//   FormField,
//   FormRoot,
//   maxLength,
//   min,
//   required,
//   validate,
// } from "@angular/forms/signals";
// import { RouterLink } from "@angular/router";
// import {
//   Api,
//   estimateColdChainBooking,
//   type BookingDropInput,
//   type BookingDto,
//   type BookingEstimateDto,
//   type CustomerDto,
//   type LocationDto,
//   type TruckTonnageClass,
// } from "@trackwise/shared/api";
// import { truckTonnageClassOptions } from "@trackwise/shared/api/enums";
// import {
//   Card,
//   Grid,
//   Stack,
//   Typography,
//   UiButton,
//   UiDateField,
//   UiFormField,
//   UiNumberField,
//   UiSelectField,
//   UiTextareaField,
//   UiTextField,
//   ValidatedForm,
// } from "@trackwise/shared/ui";
// import { TenantService } from "@/core/services/tenant.service";
// import { SearchCustomer, SearchLocation } from "@/shared/components/search";
// import { dateToIso, dateToTimeOfDay, isoToDate, timeOfDayToDate } from "@/shared/utils";
// import { BookingEstimateCard } from "../booking-estimate-card/booking-estimate-card";

// /** What the page sends: the create/update command's shape. */
// export interface ColdChainBookingFormValue {
//   customerId: string;
//   /** Only on a new booking, while numbers are typed in from nSFS. */
//   referenceNumber: string | null;
//   serviceDate: string;
//   callTime: string | null;
//   truckSize: TruckTonnageClass;
//   pickupSiteId: string | null;
//   commodity: string | null;
//   temperatureMinC: number | null;
//   temperatureMaxC: number | null;
//   notes: string | null;
//   drops: BookingDropInput[];
// }

// interface DropRow {
//   site: LocationDto | null;
//   plannedBoxes: number | null;
//   plannedBags: number | null;
//   notes: string;
// }

// interface BookingModel {
//   customer: CustomerDto | null;
//   referenceNumber: string;
//   serviceDate: Date | null;
//   callTime: Date | null;
//   truckSize: TruckTonnageClass | null;
//   pickupSite: LocationDto | null;
//   commodity: string;
//   temperatureMinC: number | null;
//   temperatureMaxC: number | null;
//   notes: string;
//   drops: DropRow[];
// }

// const emptyDrop = (): DropRow => ({ site: null, plannedBoxes: null, plannedBags: null, notes: "" });

// /** Waits for typing to settle before asking the server for a new estimate. */
// const EstimateDelayMs = 400;

// /**
//  * One Cold Chain truck-trip: the client, the day and call time, the truck size, the warehouse it
//  * loads at and the outlets it drops at. The price fills in from the client's rate agreement as the
//  * plan takes shape.
//  */
// @Component({
//   selector: "app-cold-chain-booking-form",
//   templateUrl: "./cold-chain-booking-form.html",
//   imports: [
//     BookingEstimateCard,
//     Card,
//     FormField,
//     FormRoot,
//     Grid,
//     RouterLink,
//     SearchCustomer,
//     SearchLocation,
//     Stack,
//     Typography,
//     UiButton,
//     UiDateField,
//     UiFormField,
//     UiNumberField,
//     UiSelectField,
//     UiTextareaField,
//     UiTextField,
//     ValidatedForm,
//   ],
// })
// export class ColdChainBookingForm {
//   private readonly api = inject(Api);
//   private readonly tenantService = inject(TenantService);

//   /** Edit mode: the booking being changed. The client is fixed once booked. */
//   public readonly booking = input<BookingDto | null>(null);
//   public readonly isSaving = input(false);
//   public readonly cancelLink = input<string>("/forwarding/bookings-executions");
//   public readonly save = output<ColdChainBookingFormValue>();

//   protected readonly truckSizeOptions = truckTonnageClassOptions;

//   protected readonly model = signal<BookingModel>({
//     customer: null,
//     referenceNumber: "",
//     serviceDate: null,
//     callTime: null,
//     truckSize: null,
//     pickupSite: null,
//     commodity: "",
//     temperatureMinC: null,
//     temperatureMaxC: null,
//     notes: "",
//     drops: [emptyDrop()],
//   });

//   protected readonly estimate = signal<BookingEstimateDto | null>(null);
//   protected readonly isEstimating = signal(false);

//   protected readonly isEdit = computed(() => this.booking() !== null);
//   /** A new booking takes nSFS's number here; an existing one sets it from its details page. */
//   protected readonly asksForNumber = computed(
//     () => !this.isEdit() && this.tenantService.coldChainBookingNumbersTyped(),
//   );
//   protected readonly customerId = computed(() => this.model().customer?.id ?? null);
//   protected readonly totalBoxes = computed(() =>
//     this.model().drops.reduce((sum, d) => sum + (d.plannedBoxes ?? 0), 0),
//   );
//   protected readonly totalBags = computed(() =>
//     this.model().drops.reduce((sum, d) => sum + (d.plannedBags ?? 0), 0),
//   );

//   protected readonly form = form(
//     this.model,
//     (p) => {
//       required(p.customer, { message: "Pick the client." });
//       disabled(p.customer, { when: () => this.isEdit() });
//       maxLength(p.referenceNumber, 30, { message: "Keep it under 30 characters." });
//       required(p.serviceDate, { message: "Pick the day the trip runs." });
//       required(p.truckSize, { message: "Pick the truck size - the rate depends on it." });
//       // Outlets are filtered to the client, so there is nothing to offer before one is set.
//       disabled(p.pickupSite, { when: ({ valueOf }) => !valueOf(p.customer) });
//       maxLength(p.commodity, 200, { message: "Keep it under 200 characters." });
//       maxLength(p.notes, 1000, { message: "Keep notes under 1000 characters." });
//       validate(p.temperatureMaxC, ({ value, valueOf }) => {
//         const max = value();
//         const lowest = valueOf(p.temperatureMinC);
//         return max != null && lowest != null && max < lowest
//           ? { kind: "range", message: "Can't be below the lowest." }
//           : null;
//       });

//       applyEach(p.drops, (drop) => {
//         required(drop.site, { message: "Which outlet?" });
//         disabled(drop.site, { when: () => !this.customerId() });
//         min(drop.plannedBoxes, 0, { message: "Can't be negative." });
//         min(drop.plannedBags, 0, { message: "Can't be negative." });
//         maxLength(drop.notes, 500, { message: "Keep it under 500 characters." });
//         validate(drop.site, ({ value }) => {
//           const id = value()?.id;
//           const repeats = id ? this.model().drops.filter((d) => d.site?.id === id).length : 0;
//           return repeats > 1
//             ? { kind: "duplicate", message: "Already a drop on this trip." }
//             : null;
//         });
//       });
//     },
//     {
//       submission: {
//         action: async () => {
//           this.save.emit(this.toValue());
//           return undefined;
//         },
//       },
//     },
//   );

//   constructor() {
//     effect(() => {
//       const booking = this.booking();
//       if (booking) {
//         untracked(() => this.model.set(fromDto(booking)));
//       }
//     });

//     // The server refuses another client's outlet, so drop them as soon as the client changes.
//     effect(() => {
//       const customerId = this.customerId();
//       const { pickupSite, drops } = untracked(this.model);
//       const foreign = (site: LocationDto | null) =>
//         !!site?.customerId && site.customerId !== customerId;

//       if (foreign(pickupSite) || drops.some((d) => foreign(d.site))) {
//         this.model.update((v) => ({
//           ...v,
//           pickupSite: foreign(v.pickupSite) ? null : v.pickupSite,
//           drops: v.drops.map((d) => (foreign(d.site) ? { ...d, site: null } : d)),
//         }));
//       }
//     });

//     this.watchEstimate();
//   }

//   protected addDrop(): void {
//     this.model.update((v) => ({ ...v, drops: [...v.drops, emptyDrop()] }));
//   }

//   protected removeDrop(index: number): void {
//     this.model.update((v) => ({ ...v, drops: v.drops.filter((_, i) => i !== index) }));
//   }

//   /** Re-estimates once the plan has a client, day, size and at least one outlet. */
//   private watchEstimate(): void {
//     let timer: ReturnType<typeof setTimeout> | undefined;
//     let request = 0;
//     inject(DestroyRef).onDestroy(() => clearTimeout(timer));

//     effect(() => {
//       const v = this.model();
//       const drops = v.drops.filter((d) => d.site?.id);
//       clearTimeout(timer);

//       if (!v.customer?.id || !v.serviceDate || !v.truckSize || drops.length === 0) {
//         this.estimate.set(null);
//         return;
//       }

//       const body = {
//         ...this.toValue(),
//         drops: drops.map(toDropInput),
//         bookingId: this.booking()?.id,
//       };
//       const current = ++request;
//       timer = setTimeout(async () => {
//         this.isEstimating.set(true);
//         try {
//           const estimate = await this.api.invoke(estimateColdChainBooking, { body });
//           if (current === request) {
//             this.estimate.set(estimate);
//           }
//         } catch {
//           if (current === request) {
//             this.estimate.set(null);
//           }
//         } finally {
//           if (current === request) {
//             this.isEstimating.set(false);
//           }
//         }
//       }, EstimateDelayMs);
//     });
//   }

//   private toValue(): ColdChainBookingFormValue {
//     const v = this.model();
//     return {
//       customerId: v.customer?.id ?? "",
//       referenceNumber: this.asksForNumber() ? v.referenceNumber.trim() || null : null,
//       serviceDate: dateToIso(v.serviceDate) ?? "",
//       callTime: dateToTimeOfDay(v.callTime),
//       truckSize: v.truckSize!,
//       pickupSiteId: v.pickupSite?.id ?? null,
//       commodity: v.commodity.trim() || null,
//       temperatureMinC: v.temperatureMinC,
//       temperatureMaxC: v.temperatureMaxC,
//       notes: v.notes.trim() || null,
//       drops: v.drops.map(toDropInput),
//     };
//   }
// }

// function toDropInput(drop: DropRow): BookingDropInput {
//   return {
//     siteId: drop.site?.id ?? "",
//     plannedBoxes: drop.plannedBoxes,
//     plannedBags: drop.plannedBags,
//     notes: drop.notes.trim() || null,
//   };
// }

// function fromDto(booking: BookingDto): BookingModel {
//   const drops = booking.drops ?? [];
//   return {
//     customer: { id: booking.customerId, name: booking.customerName ?? "" } as CustomerDto,
//     referenceNumber: booking.referenceNumber ?? "",
//     serviceDate: isoToDate(booking.serviceDate),
//     callTime: timeOfDayToDate(booking.callTime),
//     truckSize: booking.truckSize ?? null,
//     // The picker shows `name`; the sites are the client's own, so the owner is the booking's client.
//     pickupSite: booking.pickupSiteId
//       ? ({
//           id: booking.pickupSiteId,
//           name: booking.pickupSiteName ?? "",
//           customerId: booking.customerId,
//         } as LocationDto)
//       : null,
//     commodity: booking.commodity ?? "",
//     temperatureMinC: booking.temperatureMinC ?? null,
//     temperatureMaxC: booking.temperatureMaxC ?? null,
//     notes: booking.notes ?? "",
//     drops: drops.length
//       ? drops.map((d) => ({
//           site: {
//             id: d.siteId,
//             name: d.siteName,
//             code: d.siteCode,
//             customerId: booking.customerId,
//           } as LocationDto,
//           plannedBoxes: d.plannedBoxes ?? null,
//           plannedBags: d.plannedBags ?? null,
//           notes: d.notes ?? "",
//         }))
//       : [emptyDrop()],
//   };
// }
