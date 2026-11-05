import { Component, inject, input, model, output, signal } from "@angular/core";
import {
  disabled,
  form,
  FormField,
  FormRoot,
  maxLength,
  required,
  validate,
} from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  createBillingBatch,
  type BillingBatchDto,
  type CustomerDto,
} from "@trackwise/shared/api";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchCustomer } from "@/shared/components/search";
import { dateToIso } from "@/shared/utils";
import { billingCycleLabel } from "../../utils/billing-batch-labels";
import { lastClosedPeriod } from "../../utils/billing-period";

/** What the "ready to bill" list fills in: a client, its period, and for per-trip clients the trip. */
export interface BillingBatchPrefill {
  customer: CustomerDto;
  periodStart: Date;
  periodEnd: Date;
  tripIds?: string[];
  tripLabel?: string;
}

interface BatchModel {
  customer: CustomerDto | null;
  periodStart: Date | null;
  periodEnd: Date | null;
  notes: string;
}

/** Opens a client's billing batch: every completed trip in the period that isn't billed yet. */
@Component({
  selector: "app-billing-batch-dialog",
  templateUrl: "./billing-batch-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    SearchCustomer,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class BillingBatchDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly prefill = input<BillingBatchPrefill | null>(null);
  public readonly open = model(false);
  public readonly created = output<BillingBatchDto>();

  protected readonly model = signal<BatchModel>(emptyModel());
  protected readonly cycleLabel = billingCycleLabel;

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.customer, { message: "Pick the client." });
      disabled(p.customer, { when: () => !!this.prefill() });
      required(p.periodStart, { message: "Pick the first day of the period." });
      required(p.periodEnd, { message: "Pick the cut-off." });
      validate(p.periodEnd, ({ value, valueOf }) => {
        const start = valueOf(p.periodStart);
        const end = value();
        return start && end && end < start
          ? { kind: "range", message: "The period can't end before it starts." }
          : null;
      });
      maxLength(p.notes, 1000, { message: "Keep the remarks under 1,000 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          try {
            const batch = await this.api.invoke(createBillingBatch, {
              body: {
                customerId: v.customer!.id!,
                periodStart: dateToIso(v.periodStart)!,
                periodEnd: dateToIso(v.periodEnd)!,
                tripIds: this.prefill()?.tripIds ?? [],
                notes: v.notes.trim() || null,
              },
            });
            this.toast.showSuccess(
              `${batch.referenceNumber} opened with ${batch.tripCount} trip(s).`,
            );
            this.open.set(false);
            this.created.emit(batch);
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not open the batch."));
          }
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    const prefill = this.prefill();
    this.model.set(
      prefill
        ? {
            customer: prefill.customer,
            periodStart: prefill.periodStart,
            periodEnd: prefill.periodEnd,
            notes: "",
          }
        : emptyModel(),
    );
  }

  /** A client with an agreed cycle gets its last closed period filled in. */
  protected onCustomerPicked(customer: CustomerDto | null): void {
    if (!customer?.billingCycle || this.prefill()) {
      return;
    }

    const period = lastClosedPeriod(customer.billingCycle);
    this.model.update((m) => ({ ...m, periodStart: period.start, periodEnd: period.end }));
  }
}

function emptyModel(): BatchModel {
  const period = lastClosedPeriod("semi_monthly");
  return { customer: null, periodStart: period.start, periodEnd: period.end, notes: "" };
}
