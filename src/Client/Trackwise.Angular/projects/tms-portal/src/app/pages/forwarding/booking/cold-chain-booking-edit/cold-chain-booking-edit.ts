// import { Component, computed, inject, signal, type OnInit } from "@angular/core";
// import { ActivatedRoute, Router } from "@angular/router";
// import { getApiErrorMessage } from "@trackwise/shared";
// import {
//   Api,
//   getBookingById,
//   updateColdChainBooking,
//   type BookingDto,
// } from "@trackwise/shared/api";
// import { Spinner } from "@trackwise/shared/ui";
// import { ToastService } from "@/core/services";
// import { PageHeader } from "@/shared/components";
// import {
//   ColdChainBookingForm,
//   type ColdChainBookingFormValue,
// } from "../components/cold-chain-booking-form/cold-chain-booking-form";

// @Component({
//   selector: "app-cold-chain-booking-edit",
//   templateUrl: "./cold-chain-booking-edit.html",
//   imports: [ColdChainBookingForm, PageHeader, Spinner],
// })
// export class ColdChainBookingEdit implements OnInit {
//   private readonly api = inject(Api);
//   private readonly route = inject(ActivatedRoute);
//   private readonly router = inject(Router);
//   private readonly toast = inject(ToastService);

//   protected readonly booking = signal<BookingDto | null>(null);
//   protected readonly isSaving = signal(false);
//   protected readonly detailsLink = computed(
//     () => `/forwarding/bookings-executions/${this.booking()?.id ?? ""}`,
//   );

//   async ngOnInit(): Promise<void> {
//     const id = this.route.snapshot.paramMap.get("id");
//     if (!id) {
//       return;
//     }

//     try {
//       this.booking.set(await this.api.invoke(getBookingById, { id }));
//     } catch {
//       this.toast.showError("Failed to load booking.");
//     }
//   }

//   protected async update(value: ColdChainBookingFormValue): Promise<void> {
//     const id = this.booking()?.id;
//     if (!id) {
//       return;
//     }

//     this.isSaving.set(true);
//     try {
//       await this.api.invoke(updateColdChainBooking, { id, body: value });
//       this.toast.showSuccess("Booking updated.");
//       await this.router.navigateByUrl(this.detailsLink());
//     } catch (error) {
//       this.toast.showError(getApiErrorMessage(error, "Could not save the booking."));
//     } finally {
//       this.isSaving.set(false);
//     }
//   }
// }
