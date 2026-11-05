import { Component, inject, signal } from "@angular/core";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  approveExpense,
  deleteExpense,
  downloadExpenseReceipt,
  getFuelSummary,
  getTrucks,
  rejectExpense,
  type ExpenseStatus,
  type FleetCard,
  type FuelFillUpDto,
  type FuelSummaryDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { fleetCardOptions, fuelReviewStatusOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import {
  Card,
  DateRangePicker,
  Grid,
  UiButton,
  UiDialog,
  UiFormField,
  UiSelectField,
  UiTab,
  UiTabList,
  UiTabPanel,
  UiTabPanels,
  UiTabs,
  UiTextareaField,
} from "@trackwise/shared/ui";
import { downloadBlobFile } from "@trackwise/shared/utils";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { dateToIso } from "@/shared/utils";
import { FuelFillUpDialog } from "../components/fuel-fill-up-dialog/fuel-fill-up-dialog";
import { FuelFillUpsTable } from "../components/fuel-fill-ups-table/fuel-fill-ups-table";
import { FuelSummary } from "../components/fuel-summary/fuel-summary";
import { FuelTrucksTable } from "../components/fuel-trucks-table/fuel-trucks-table";
import { FuelFillUpsStore } from "../store/fuel-fill-ups.store";

/**
 * Every fill-up, typed from its receipt - what Monday's Fleet Gas Expenses board and the fuel
 * columns on CCL OPS - DISPATCH held. The totals follow the same filters as the list.
 */
@Component({
  selector: "app-fuel-log-list",
  templateUrl: "./fuel-log-list.html",
  providers: [FuelFillUpsStore],
  imports: [
    Card,
    DateRangePicker,
    FuelFillUpDialog,
    FuelFillUpsTable,
    FuelSummary,
    FuelTrucksTable,
    Grid,
    PageHeader,
    UiButton,
    UiDialog,
    UiFormField,
    UiSelectField,
    UiTab,
    UiTabList,
    UiTabPanel,
    UiTabPanels,
    UiTabs,
    UiTextareaField,
  ],
})
export class FuelLogList {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(FuelFillUpsStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Expense.Manage);
  protected readonly canReview = this.permissions.hasPermission(Permission.Expense.Approve);

  protected readonly cardOptions = fleetCardOptions;
  protected readonly statusOptions = fuelReviewStatusOptions;
  protected readonly trucks = signal<TruckDto[]>([]);

  protected readonly period = signal<Date[]>(thisMonth());
  protected readonly truckId = signal<string | null>(null);
  protected readonly fleetCard = signal<FleetCard | null>(null);
  protected readonly status = signal<ExpenseStatus | null>(null);

  protected readonly summary = signal<FuelSummaryDto | null>(null);
  protected readonly tab = signal("fills");
  protected readonly busy = signal(false);

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<FuelFillUpDto | null>(null);
  protected readonly flagging = signal<FuelFillUpDto | null>(null);
  protected readonly flagReason = signal("");

  constructor() {
    this.store.setFilters(this.filters(), { reload: false });
    void this.loadSummary();
    void this.loadTrucks();
  }

  protected onPeriodChange(dates: Date[]): void {
    this.period.set(dates.length ? dates : thisMonth());
    this.refresh();
  }

  protected onTruckChange(truckId: string | null): void {
    this.truckId.set(truckId);
    this.refresh();
  }

  protected onCardChange(card: FleetCard | null): void {
    this.fleetCard.set(card);
    this.refresh();
  }

  protected onStatusChange(status: ExpenseStatus | null): void {
    this.status.set(status);
    this.refresh();
  }

  protected openNew(): void {
    this.editing.set(null);
    this.dialogOpen.set(true);
  }

  protected openEdit(fill: FuelFillUpDto): void {
    this.editing.set(fill);
    this.dialogOpen.set(true);
  }

  protected onSaved(): void {
    this.store.retry();
    void this.loadSummary();
  }

  protected verify(fill: FuelFillUpDto): void {
    void this.run(() => this.api.invoke(approveExpense, { id: fill.id! }), "Fill-up verified");
  }

  protected openFlag(fill: FuelFillUpDto): void {
    this.flagReason.set("");
    this.flagging.set(fill);
  }

  protected async confirmFlag(): Promise<void> {
    const fill = this.flagging();
    const reason = this.flagReason().trim();
    if (!fill || !reason) {
      return;
    }

    const ok = await this.run(
      () => this.api.invoke(rejectExpense, { id: fill.id!, body: { reason } }),
      "Fill-up flagged",
    );
    if (ok) {
      this.flagging.set(null);
    }
  }

  protected remove(fill: FuelFillUpDto): void {
    this.toast.confirm({
      header: "Delete fill-up",
      message: `Delete the ${fill.truckNumber ?? ""} fill-up of ${fill.liters} L?`,
      acceptLabel: "Delete",
      severity: "danger",
      accept: () =>
        this.run(() => this.api.invoke(deleteExpense, { id: fill.id! }), "Fill-up deleted"),
    });
  }

  protected async openReceipt(fill: FuelFillUpDto): Promise<void> {
    try {
      const blob = await this.api.invoke(downloadExpenseReceipt, { id: fill.id! });
      if (blob) {
        downloadBlobFile(blob, `fuel-${fill.number}`);
      }
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Could not open the receipt."));
    }
  }

  private refresh(): void {
    this.store.setFilters(this.filters());
    void this.loadSummary();
  }

  private filters(): Record<string, unknown> {
    const [from, to] = this.period();
    return {
      From: dateToIso(from) ?? undefined,
      To: dateToIso(to ?? from) ?? undefined,
      TruckId: this.truckId() ?? undefined,
      FleetCard: this.fleetCard() ?? undefined,
      Status: this.status() ?? undefined,
    };
  }

  private async loadSummary(): Promise<void> {
    try {
      this.summary.set(await this.api.invoke(getFuelSummary, this.filters()));
    } catch (error) {
      this.summary.set(null);
      this.toast.showError(getApiErrorMessage(error, "Could not total the fuel log."));
    }
  }

  private async loadTrucks(): Promise<void> {
    const result = await this.api.invoke(getTrucks, { PageSize: 200, OrderBy: "Number" });
    this.trucks.set(result?.items ?? []);
  }

  private async run(action: () => Promise<unknown>, success: string): Promise<boolean> {
    this.busy.set(true);
    try {
      await action();
      this.toast.showSuccess(success);
      this.store.retry();
      void this.loadSummary();
      return true;
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "That didn't work."));
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}

function thisMonth(): Date[] {
  const today = new Date();
  return [new Date(today.getFullYear(), today.getMonth(), 1), today];
}
