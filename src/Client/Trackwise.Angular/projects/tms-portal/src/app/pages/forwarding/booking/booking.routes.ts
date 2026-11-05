import type { Routes } from "@angular/router";
import { authGuard } from "@/core/auth";
import { BookingDetailsComponent } from "./booking-details/booking-details";
import { BookingsListComponent } from "./bookings-list/bookings-list";

// import { ColdChainBookingAdd } from "./cold-chain-booking-add/cold-chain-booking-add";
// import { ColdChainBookingEdit } from "./cold-chain-booking-edit/cold-chain-booking-edit";

export const BookingRoutes: Routes = [
  {
    path: "",
    component: BookingsListComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "",
      permission: "Permission.Booking.View",
    },
  },
  // {
  //   path: "new",
  //   component: ColdChainBookingAdd,
  //   canActivate: [authGuard],
  //   data: {
  //     breadcrumb: "New Cold Chain Booking",
  //     permission: "Permission.Booking.Manage",
  //   },
  // },
  // {
  //   path: ":id/edit",
  //   component: ColdChainBookingEdit,
  //   canActivate: [authGuard],
  //   data: {
  //     breadcrumb: "Edit Booking",
  //     permission: "Permission.Booking.Manage",
  //   },
  // },
  {
    path: ":id",
    component: BookingDetailsComponent,
    canActivate: [authGuard],
    data: {
      breadcrumb: "Booking Details",
      permission: "Permission.Booking.View",
    },
  },
];
