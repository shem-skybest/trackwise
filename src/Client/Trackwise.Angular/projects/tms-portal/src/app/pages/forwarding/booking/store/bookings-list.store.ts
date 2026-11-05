import { getBookings, type BookingSummaryDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

export const BookingsListStore = createListStore<BookingSummaryDto>(getBookings, {
  defaultSortField: "CreatedAt",
  defaultPageSize: 10,
});
