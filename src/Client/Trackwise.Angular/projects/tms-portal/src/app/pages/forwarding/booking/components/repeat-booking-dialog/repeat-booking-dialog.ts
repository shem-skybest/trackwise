// import { DatePipe } from "@angular/common";
// import { Component, computed, inject, input, model, signal } from "@angular/core";
// import { getApiErrorMessage } from "@trackwise/shared";
// import { Api, repeatColdChainBooking, type BookingDto } from "@trackwise/shared/api";
// import {
//   Alert,
//   Stack,
//   Typography,
//   UiButton,
//   UiCheckboxField,
//   UiDateField,
//   UiDialog,
//   UiFormField,
//   UiMultiSelectField,
// } from "@trackwise/shared/ui";
// import { ToastService } from "@/core/services";
// import { dateToIso, isoToDate } from "@/shared/utils";
// import { MAX_REPEAT_DAYS, standingOrderDates } from "../../utils/standing-order-dates";

// const WEEKDAYS = [
//   { label: "Monday", value: 1 },
//   { label: "Tuesday", value: 2 },
//   { label: "Wednesday", value: 3 },
//   { label: "Thursday", value: 4 },
//   { label: "Friday", value: 5 },
//   { label: "Saturday", value: 6 },
//   { label: "Sunday", value: 0 },
// ];

// const PREVIEW_LIMIT = 8;

// /**
//  * A standing order: the same client, warehouse, drops and truck size booked again on the chosen
//  * weekdays, each day its own booking with its own BKN-RFV number. HFI books every Monday.
//  */
// @Component({
//   selector: "app-repeat-booking-dialog",
//   templateUrl: "./repeat-booking-dialog.html",
//   imports: [
//     Alert,
//     DatePipe,
//     Stack,
//     Typography,
//     UiButton,
//     UiCheckboxField,
//     UiDateField,
//     UiDialog,
//     UiFormField,
//     UiMultiSelectField,
//   ],
// })
// export class RepeatBookingDialog {
//   private readonly api = inject(Api);
//   private readonly toast = inject(ToastService);

//   public readonly booking = input.required<BookingDto>();
//   public readonly open = model(false);

//   protected readonly weekdayOptions = WEEKDAYS;
//   protected readonly maxDays = MAX_REPEAT_DAYS;

//   protected readonly from = signal<Date | null>(null);
//   protected readonly until = signal<Date | null>(null);
//   protected readonly weekdays = signal<number[]>([]);
//   protected readonly confirmNow = signal(false);
//   protected readonly isBusy = signal(false);

//   protected readonly dates = computed(() => {
//     const from = this.from();
//     const until = this.until();
//     return from && until ? standingOrderDates(from, until, this.weekdays()) : [];
//   });

//   protected readonly tooMany = computed(() => this.dates().length > MAX_REPEAT_DAYS);
//   protected readonly preview = computed(() => this.dates().slice(0, PREVIEW_LIMIT));
//   protected readonly moreCount = computed(() => Math.max(0, this.dates().length - PREVIEW_LIMIT));

//   protected onOpened(): void {
//     const booked = isoToDate(this.booking().completedAt) ?? startOfToday();
//     const tomorrow = addDays(startOfToday(), 1);
//     const nextWeek = addDays(booked, 7);
//     const from = nextWeek > tomorrow ? nextWeek : tomorrow;

//     this.from.set(from);
//     this.until.set(addDays(from, 27));
//     this.weekdays.set([booked.getDay()]);
//     this.confirmNow.set(false);
//   }

//   protected async repeat(): Promise<void> {
//     const dates = this.dates();
//     if (this.isBusy() || dates.length === 0 || this.tooMany()) {
//       return;
//     }

//     this.isBusy.set(true);
//     try {
//       const made = await this.api.invoke(repeatColdChainBooking, {
//         id: this.booking().id!,
//         body: { dates: dates.map((d) => dateToIso(d)!), confirm: this.confirmNow() },
//       });

//       const first = made[0]?.referenceNumber;
//       const last = made[made.length - 1]?.referenceNumber;
//       // While numbers are typed in from nSFS the copies have none yet.
//       const range = !first ? null : made.length > 1 ? `${first} to ${last}` : first;
//       this.toast.showSuccess(
//         `${made.length} booking${made.length === 1 ? "" : "s"} made${range ? `: ${range}` : ""}.`,
//       );
//       this.open.set(false);
//     } catch (error) {
//       this.toast.showError(getApiErrorMessage(error, "Could not repeat the booking."));
//     } finally {
//       this.isBusy.set(false);
//     }
//   }
// }

// function startOfToday(): Date {
//   const now = new Date();
//   return new Date(now.getFullYear(), now.getMonth(), now.getDate());
// }

// function addDays(date: Date, days: number): Date {
//   return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
// }
