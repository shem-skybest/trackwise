import { DatePipe } from "@angular/common";
import { Component, computed, effect, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, min, required } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addCashFundExpense,
  Api,
  getTrips,
  getTrucks,
  updateCashFundExpense,
  type CashExpenseCategory,
  type CashFundDto,
  type CashFundExpenseDto,
  type EmployeeDto,
  type TripDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { cashExpenseCategoryOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchEmployee } from "@/shared/components/search";
import { dateToIso, isoToDate } from "@/shared/utils";

interface ExpenseModel {
  spentOn: Date | null;
  category: CashExpenseCategory;
  description: string;
  amount: number | null;
  receiptNumber: string;
  truckId: string | null;
  tripId: string | null;
  spentBy: EmployeeDto | null;
}

interface TripOption {
  id: string;
  label: string;
  trip: TripDto;
}

/**
 * Adds or corrects one fund expense. Picking a truck lists its recent trips; picking a trip fills
 * in its driver as the one who paid, the way the liquidation board names the driver on each row.
 */
@Component({
  selector: "app-cash-expense-dialog",
  templateUrl: "./cash-expense-dialog.html",
  providers: [DatePipe],
  imports: [
    FormField,
    FormRoot,
    Grid,
    SearchEmployee,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class CashExpenseDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly datePipe = inject(DatePipe);

  public readonly fundId = input.required<string>();

  /** The expense to correct; a new one when null. */
  public readonly expense = input<CashFundExpenseDto | null>(null);
  public readonly open = model(false);
  public readonly saved = output<CashFundDto>();

  protected readonly categoryOptions = cashExpenseCategoryOptions;
  protected readonly trucks = signal<TruckDto[]>([]);
  protected readonly trips = signal<TripOption[]>([]);
  protected readonly loadingTrips = signal(false);
  protected readonly isEdit = computed(() => this.expense() !== null);
  protected readonly model = signal<ExpenseModel>(emptyModel());
  private readonly truckId = computed(() => this.model().truckId);

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.spentOn, { message: "Pick the day it was paid." });
      required(p.category, { message: "Pick what it was for." });
      required(p.description, { message: "Say what it was for, e.g. PFI passway." });
      maxLength(p.description, 200, { message: "Keep it under 200 characters." });
      required(p.amount, { message: "Enter the amount." });
      min(p.amount, 0.01, { message: "The amount must be above zero." });
      maxLength(p.receiptNumber, 64, { message: "Keep the receipt number under 64 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            spentOn: dateToIso(v.spentOn)!,
            category: v.category,
            description: v.description.trim(),
            amount: v.amount!,
            receiptNumber: v.receiptNumber.trim() || null,
            truckId: v.truckId,
            tripId: v.tripId,
            spentById: v.spentBy?.id ?? null,
          };

          try {
            const existing = this.expense();
            const fund = existing
              ? await this.api.invoke(updateCashFundExpense, {
                  id: this.fundId(),
                  expenseId: existing.id!,
                  body,
                })
              : await this.api.invoke(addCashFundExpense, { id: this.fundId(), body });
            this.toast.showSuccess(existing ? "Expense updated." : "Expense added.");
            this.saved.emit(fund);
            if (existing) {
              this.open.set(false);
            } else {
              this.startNext(v);
            }
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not save the expense."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => void this.loadTrips(this.truckId()));
  }

  protected async onOpened(): Promise<void> {
    const expense = this.expense();
    this.model.set(
      expense
        ? {
            spentOn: isoToDate(expense.spentOn),
            category: expense.category ?? "passway",
            description: expense.description ?? "",
            amount: expense.amount ?? null,
            receiptNumber: expense.receiptNumber ?? "",
            truckId: expense.truckId ?? null,
            tripId: expense.tripId ?? null,
            spentBy: expense.spentById
              ? ({ id: expense.spentById, fullName: expense.spentByName } as EmployeeDto)
              : null,
          }
        : emptyModel(),
    );

    if (this.trucks().length === 0) {
      const result = await this.api.invoke(getTrucks, { PageSize: 200, OrderBy: "Number" });
      this.trucks.set(result?.items ?? []);
    }
  }

  protected onTripChange(tripId: string | null): void {
    const trip = this.trips().find((t) => t.id === tripId)?.trip;
    this.model.update((v) => ({
      ...v,
      tripId,
      spentBy:
        v.spentBy ??
        (trip?.driverId ? ({ id: trip.driverId, fullName: trip.driverName } as EmployeeDto) : null),
    }));
  }

  /** Keeps the dialog open for the next row, as Ops types a whole trip's receipts in one go. */
  private startNext(previous: ExpenseModel): void {
    this.model.set({
      ...emptyModel(),
      spentOn: previous.spentOn,
      truckId: previous.truckId,
      tripId: previous.tripId,
      spentBy: previous.spentBy,
    });
    this.form().reset();
  }

  private async loadTrips(truckId: string | null): Promise<void> {
    if (!truckId) {
      this.trips.set([]);
      return;
    }

    this.loadingTrips.set(true);
    try {
      const result = await this.api.invoke(getTrips, {
        TruckId: truckId,
        OrderBy: "-CreatedAt",
        PageSize: 30,
      });
      this.trips.set(
        (result?.items ?? []).map((trip) => ({
          id: trip.id!,
          label: `#${trip.number} · ${this.datePipe.transform(trip.createdAt, "MMM d")} · ${trip.name}`,
          trip,
        })),
      );
    } finally {
      this.loadingTrips.set(false);
    }
  }
}

function emptyModel(): ExpenseModel {
  return {
    spentOn: new Date(),
    category: "passway",
    description: "",
    amount: null,
    receiptNumber: "",
    truckId: null,
    tripId: null,
    spentBy: null,
  };
}
