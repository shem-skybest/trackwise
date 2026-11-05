import { getExpenses, type ExpenseDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the expenses list page.
 */
export const ExpensesListStore = createListStore<ExpenseDto>(getExpenses, {
  defaultSortField: "-ExpenseDate",
  defaultPageSize: 10,
});
