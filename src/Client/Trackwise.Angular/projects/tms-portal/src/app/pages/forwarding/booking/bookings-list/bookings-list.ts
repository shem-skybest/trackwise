import { Component, inject } from "@angular/core";
import { Permission } from "@trackwise/shared/models/permissions";
import { PermissionService } from "@/core/auth";
import { PageHeader } from "@/shared/components";
import { BookingsTable } from "../components/bookings-table/bookings-table";
import { BookingsListStore } from "../store/bookings-list.store";

@Component({
  selector: "app-bookings-list",
  templateUrl: "./bookings-list.html",
  providers: [BookingsListStore],
  imports: [PageHeader, BookingsTable],
})
export class BookingsListComponent {
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(BookingsListStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Booking.Manage);
}
