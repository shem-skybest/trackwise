import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, signal, viewChild } from "@angular/core";
import { Router } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  deleteCashFund,
  deleteCashFundExpense,
  forwardCashFundExpenses,
  getCashFundById,
  liquidateCashFund,
  reopenCashFund,
  reviewCashFundExpenses,
  type CashFundDto,
  type CashFundExpenseDto,
} from "@trackwise/shared/api";
import { Permission } from "@trackwise/shared/models/permissions";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  EmptyState,
  Grid,
  InfoItem,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiTextareaField,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { isoToDate } from "@/shared/utils";
import { CashExpenseDialog } from "../components/cash-expense-dialog/cash-expense-dialog";
import { CashExpensesTable } from "../components/cash-expenses-table/cash-expenses-table";
import { CashFundDialog } from "../components/cash-fund-dialog/cash-fund-dialog";
import { fundStatusIntent, fundStatusLabel } from "../utils/cash-fund-labels";

@Component({
  selector: "app-cash-fund-details",
  templateUrl: "./cash-fund-details.html",
  imports: [
    Badge,
    Card,
    CashExpenseDialog,
    CashExpensesTable,
    CashFundDialog,
    CurrencyFormatPipe,
    DatePipe,
    EmptyState,
    Grid,
    InfoItem,
    PageHeader,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiTextareaField,
  ],
})
export class CashFundDetails {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);
  private readonly table = viewChild(CashExpensesTable);

  public readonly id = input.required<string>();

  protected readonly fund = signal<CashFundDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);

  protected readonly fundDialogOpen = signal(false);
  protected readonly expenseDialogOpen = signal(false);
  protected readonly editingExpense = signal<CashFundExpenseDto | null>(null);

  protected readonly disallowOpen = signal(false);
  protected readonly disallowIds = signal<string[]>([]);
  protected readonly disallowReason = signal("");

  protected readonly canManage = this.permissions.hasPermission(Permission.Expense.Manage);
  protected readonly canReview = this.permissions.hasPermission(Permission.Expense.Approve);
  protected readonly isOpen = computed(() => this.fund()?.status === "open");
  protected readonly toReview = computed(() => {
    const fund = this.fund();
    return fund ? (fund.withOpsCount ?? 0) + (fund.forwardedCount ?? 0) : 0;
  });
  protected readonly canDelete = computed(() => {
    const fund = this.fund();
    return !!fund && this.isOpen() && (fund.expenses ?? []).every((e) => e.status === "with_ops");
  });

  protected readonly statusLabel = fundStatusLabel;
  protected readonly statusIntent = fundStatusIntent;
  protected readonly isoToDate = isoToDate;

  constructor() {
    effect(() => void this.load(this.id()));
  }

  protected addExpense(): void {
    this.editingExpense.set(null);
    this.expenseDialogOpen.set(true);
  }

  protected editExpense(expense: CashFundExpenseDto): void {
    this.editingExpense.set(expense);
    this.expenseDialogOpen.set(true);
  }

  protected removeExpense(expense: CashFundExpenseDto): void {
    this.toast.confirm({
      header: "Delete expense",
      message: `Delete "${expense.description}"?`,
      acceptLabel: "Delete",
      severity: "danger",
      accept: () =>
        this.run(
          () => this.api.invoke(deleteCashFundExpense, { id: this.id(), expenseId: expense.id! }),
          "Expense deleted",
        ),
    });
  }

  protected forward(expenseIds: string[]): void {
    void this.run(
      () => this.api.invoke(forwardCashFundExpenses, { id: this.id(), body: { expenseIds } }),
      `${expenseIds.length} receipt(s) forwarded to the office`,
    );
  }

  protected forwardAll(): void {
    void this.run(
      () => this.api.invoke(forwardCashFundExpenses, { id: this.id(), body: { expenseIds: [] } }),
      "Receipts forwarded to the office",
    );
  }

  protected check(expenseIds: string[]): void {
    void this.run(
      () =>
        this.api.invoke(reviewCashFundExpenses, {
          id: this.id(),
          body: { expenseIds, accept: true },
        }),
      `${expenseIds.length} receipt(s) checked`,
    );
  }

  protected openDisallow(expenseIds: string[]): void {
    this.disallowIds.set(expenseIds);
    this.disallowReason.set("");
    this.disallowOpen.set(true);
  }

  protected async confirmDisallow(): Promise<void> {
    const reason = this.disallowReason().trim();
    if (!reason) {
      return;
    }

    const expenseIds = this.disallowIds();
    const ok = await this.run(
      () =>
        this.api.invoke(reviewCashFundExpenses, {
          id: this.id(),
          body: { expenseIds, accept: false, reason },
        }),
      `${expenseIds.length} receipt(s) disallowed`,
    );
    if (ok) {
      this.disallowOpen.set(false);
    }
  }

  protected liquidate(): void {
    const fund = this.fund();
    if (!fund) {
      return;
    }

    this.toast.confirm({
      header: "Liquidate fund",
      message: this.liquidationMessage(fund),
      acceptLabel: "Liquidate",
      accept: () =>
        this.run(() => this.api.invoke(liquidateCashFund, { id: this.id() }), "Fund liquidated"),
    });
  }

  protected reopen(): void {
    this.toast.confirm({
      header: "Reopen fund",
      message: "Reopen this fund to add or review receipts again?",
      acceptLabel: "Reopen",
      accept: () =>
        this.run(() => this.api.invoke(reopenCashFund, { id: this.id() }), "Fund reopened"),
    });
  }

  protected remove(): void {
    this.toast.confirm({
      header: "Delete fund",
      message: "Delete this fund and every expense on it?",
      acceptLabel: "Delete",
      severity: "danger",
      accept: async () => {
        try {
          await this.api.invoke(deleteCashFund, { id: this.id() });
          this.toast.showSuccess("Fund deleted");
          await this.router.navigate(["/cash-funds"]);
        } catch (error) {
          this.toast.showError(getApiErrorMessage(error, "Could not delete the fund."));
        }
      },
    });
  }

  protected onSaved(fund: CashFundDto): void {
    this.fund.set(fund);
  }

  private liquidationMessage(fund: CashFundDto): string {
    const balance = fund.balance ?? 0;
    const settle =
      balance > 0
        ? `${balance.toFixed(2)} is to be returned.`
        : balance < 0
          ? `${(-balance).toFixed(2)} is owed to the custodian.`
          : "The advance is fully spent.";
    const owed = fund.disallowedTotal
      ? ` The crew owes ${fund.disallowedTotal.toFixed(2)} in disallowed receipts.`
      : "";
    return `${settle}${owed} The fund will be locked.`;
  }

  private async run(
    action: () => Promise<CashFundDto | unknown>,
    success: string,
  ): Promise<boolean> {
    this.isBusy.set(true);
    try {
      const result = await action();
      if (result && typeof result === "object" && "id" in result) {
        this.fund.set(result as CashFundDto);
      } else {
        await this.load(this.id());
      }
      this.table()?.clearSelection();
      this.toast.showSuccess(success);
      return true;
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "That didn't work."));
      return false;
    } finally {
      this.isBusy.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      this.fund.set(await this.api.invoke(getCashFundById, { id }));
    } catch {
      this.fund.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }
}
