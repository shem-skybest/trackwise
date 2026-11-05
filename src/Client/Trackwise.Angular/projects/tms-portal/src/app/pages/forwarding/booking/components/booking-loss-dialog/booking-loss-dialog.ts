// import { Component, computed, inject, input, model, output, signal } from "@angular/core";
// import { getApiErrorMessage } from "@trackwise/shared";
// import {
//   Api,
//   cancelBooking,
//   setBookingOperationalStatus,
//   type BookingLossReason,
//   type CancellationParty,
// } from "@trackwise/shared/api";
// import { bookingLossReasonOptions, cancellationPartyOptions } from "@trackwise/shared/api/enums";
// import {
//   Stack,
//   UiButton,
//   UiDialog,
//   UiFormField,
//   UiSelectField,
//   UiTextareaField,
// } from "@trackwise/shared/ui";
// import { ToastService } from "@/core/services";

// export type BookingLossKind = "cancel" | "lost";

// /**
//  * Cancels a Cold Chain booking or marks it lost, with the reason picked from a list so lost trips
//  * can be counted by cause - what Monday's free-text remarks can't do.
//  */
// @Component({
//   selector: "app-booking-loss-dialog",
//   templateUrl: "./booking-loss-dialog.html",
//   imports: [Stack, UiButton, UiDialog, UiFormField, UiSelectField, UiTextareaField],
// })
// export class BookingLossDialog {
//   private readonly api = inject(Api);
//   private readonly toast = inject(ToastService);

//   public readonly bookingId = input.required<string>();
//   public readonly kind = input<BookingLossKind>("cancel");
//   public readonly open = model(false);
//   public readonly done = output<void>();

//   protected readonly reasonOptions = bookingLossReasonOptions;
//   protected readonly partyOptions = cancellationPartyOptions;

//   protected readonly reason = signal<BookingLossReason | null>(null);
//   protected readonly party = signal<CancellationParty>("company");
//   protected readonly note = signal("");
//   protected readonly isBusy = signal(false);

//   protected readonly isCancel = computed(() => this.kind() === "cancel");
//   protected readonly header = computed(() => (this.isCancel() ? "Cancel booking" : "Mark as lost"));

//   protected onOpened(): void {
//     this.reason.set(null);
//     this.party.set("company");
//     this.note.set("");
//   }

//   protected async confirm(): Promise<void> {
//     const lossReason = this.reason();
//     if (!lossReason || this.isBusy()) {
//       return;
//     }

//     const note = this.note().trim() || null;
//     this.isBusy.set(true);
//     try {
//       if (this.isCancel()) {
//         await this.api.invoke(cancelBooking, {
//           id: this.bookingId(),
//           body: { reason: note, cancelledByParty: this.party(), lossReason },
//         });
//         this.toast.showSuccess("Booking cancelled.");
//       } else {
//         await this.api.invoke(setBookingOperationalStatus, {
//           id: this.bookingId(),
//           body: { status: "lost_opportunity", note, lossReason },
//         });
//         this.toast.showSuccess("Booking marked as lost.");
//       }

//       this.open.set(false);
//       this.done.emit();
//     } catch (error) {
//       this.toast.showError(getApiErrorMessage(error, "Could not update the booking."));
//     } finally {
//       this.isBusy.set(false);
//     }
//   }
// }
