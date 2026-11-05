import { DatePipe } from "@angular/common";
import { Component, computed, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { CashFundExpenseDto } from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Stack,
  UiButton,
  UiDataTable,
  UiTableRowDirectives,
  UiTooltip,
} from "@trackwise/shared/ui";
import { isoToDate } from "@/shared/utils";
import {
  categoryLabel,
  expenseStatusIntent,
  expenseStatusLabel,
} from "../../utils/cash-fund-labels";

/** The fund's expenses, with the bulk actions each role can take on the rows it selects. */
@Component({
  selector: "app-cash-expenses-table",
  templateUrl: "./cash-expenses-table.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DatePipe,
    RouterLink,
    Stack,
    UiButton,
    UiDataTable,
    UiTableRowDirectives,
    UiTooltip,
  ],
})
export class CashExpensesTable {
  public readonly expenses = input.required<readonly CashFundExpenseDto[]>();

  /** Ops: add, correct and forward. */
  public readonly canManage = input(false);

  /** The office: check and disallow. */
  public readonly canReview = input(false);

  public readonly busy = input(false);

  public readonly edit = output<CashFundExpenseDto>();
  public readonly remove = output<CashFundExpenseDto>();
  public readonly forward = output<string[]>();
  public readonly check = output<string[]>();
  public readonly disallow = output<string[]>();

  protected readonly selection = signal<CashFundExpenseDto[]>([]);
  protected readonly selectable = computed(() => this.canManage() || this.canReview());

  protected readonly selectedWithOps = computed(() => this.idsWith("with_ops"));
  protected readonly selectedReviewable = computed(() =>
    this.selection()
      .filter((e) => e.status !== "with_ops")
      .map((e) => e.id!),
  );

  protected readonly categoryLabel = categoryLabel;
  protected readonly statusLabel = expenseStatusLabel;
  protected readonly statusIntent = expenseStatusIntent;
  protected readonly isoToDate = isoToDate;

  protected setSelection(rows: CashFundExpenseDto[]): void {
    this.selection.set(rows);
  }

  /** Clears the selection once the page has applied an action to it. */
  public clearSelection(): void {
    this.selection.set([]);
  }

  private idsWith(status: CashFundExpenseDto["status"]): string[] {
    return this.selection()
      .filter((e) => e.status === status)
      .map((e) => e.id!);
  }
}
