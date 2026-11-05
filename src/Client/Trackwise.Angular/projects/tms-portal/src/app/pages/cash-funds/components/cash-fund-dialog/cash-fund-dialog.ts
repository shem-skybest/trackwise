import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  required,
  validate,
} from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  createCashFund,
  updateCashFund,
  type CashFundDto,
  type EmployeeDto,
} from "@trackwise/shared/api";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchEmployee } from "@/shared/components/search";
import { dateToIso, isoToDate } from "@/shared/utils";

interface FundModel {
  custodian: EmployeeDto | null;
  periodStart: Date | null;
  periodEnd: Date | null;
  amount: number | null;
  advanceReference: string;
  notes: string;
}

/** Creates an operational fund, or corrects an open one's header. */
@Component({
  selector: "app-cash-fund-dialog",
  templateUrl: "./cash-fund-dialog.html",
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
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class CashFundDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  /** The fund to edit; a new fund when null. */
  public readonly fund = input<CashFundDto | null>(null);
  public readonly open = model(false);
  public readonly saved = output<CashFundDto>();

  protected readonly isEdit = computed(() => this.fund() !== null);
  protected readonly model = signal<FundModel>(emptyModel());

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.custodian, { message: "Pick who holds the fund." });
      required(p.periodStart, { message: "Pick the first day of the period." });
      required(p.periodEnd, { message: "Pick the last day of the period." });
      validate(p.periodEnd, ({ value, valueOf }) => {
        const start = valueOf(p.periodStart);
        const end = value();
        return start && end && end < start
          ? { kind: "range", message: "The period can't end before it starts." }
          : null;
      });
      required(p.amount, { message: "Enter the advance released." });
      min(p.amount, 0, { message: "The advance can't be negative." });
      maxLength(p.advanceReference, 64, { message: "Keep the reference under 64 characters." });
      maxLength(p.notes, 500, { message: "Keep the notes under 500 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            custodianId: v.custodian!.id!,
            periodStart: dateToIso(v.periodStart)!,
            periodEnd: dateToIso(v.periodEnd)!,
            amount: v.amount!,
            advanceReference: v.advanceReference.trim() || null,
            notes: v.notes.trim() || null,
          };

          try {
            const existing = this.fund();
            const saved = existing
              ? await this.api.invoke(updateCashFund, { id: existing.id!, body })
              : await this.api.invoke(createCashFund, { body });
            this.toast.showSuccess(existing ? "Fund updated." : "Fund created.");
            this.open.set(false);
            this.saved.emit(saved);
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not save the fund."));
          }
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    const fund = this.fund();
    this.model.set(
      fund
        ? {
            custodian: { id: fund.custodianId, fullName: fund.custodianName } as EmployeeDto,
            periodStart: isoToDate(fund.periodStart),
            periodEnd: isoToDate(fund.periodEnd),
            amount: fund.amount ?? null,
            advanceReference: fund.advanceReference ?? "",
            notes: fund.notes ?? "",
          }
        : emptyModel(),
    );
  }
}

/** A new fund covers the current half of the month, the way Cold Chain's advances run. */
function emptyModel(): FundModel {
  const today = new Date();
  const firstHalf = today.getDate() <= 15;
  const year = today.getFullYear();
  const month = today.getMonth();
  return {
    custodian: null,
    periodStart: new Date(year, month, firstHalf ? 1 : 16),
    periodEnd: firstHalf ? new Date(year, month, 15) : new Date(year, month + 1, 0),
    amount: null,
    advanceReference: "",
    notes: "",
  };
}
