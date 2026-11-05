// import { Component, inject, signal } from "@angular/core";
// import { Router } from "@angular/router";
// import { getApiErrorMessage } from "@trackwise/shared";
// import { Api, createColdChainBooking } from "@trackwise/shared/api";
// import { ToastService } from "@/core/services";
// import { PageHeader } from "@/shared/components";
// import {
//   ColdChainBookingForm,
//   type ColdChainBookingFormValue,
// } from "../components/cold-chain-booking-form/cold-chain-booking-form";

// @Component({
//   selector: "app-cold-chain-booking-add",
//   templateUrl: "./cold-chain-booking-add.html",
//   imports: [ColdChainBookingForm, PageHeader],
// })
// export class ColdChainBookingAdd {
//   private readonly api = inject(Api);
//   private readonly router = inject(Router);
//   private readonly toast = inject(ToastService);

//   protected readonly isSaving = signal(false);

//   protected async create(value: ColdChainBookingFormValue): Promise<void> {
//     this.isSaving.set(true);
//     try {
//       const id = await this.api.invoke(createColdChainBooking, { body: value });
//       this.toast.showSuccess("Booked. Confirm it once the client does.");
//       await this.router.navigate(["/forwarding/bookings-executions", id]);
//     } catch (error) {
//       this.toast.showError(getApiErrorMessage(error, "Could not save the booking."));
//     } finally {
//       this.isSaving.set(false);
//     }
//   }
// }
