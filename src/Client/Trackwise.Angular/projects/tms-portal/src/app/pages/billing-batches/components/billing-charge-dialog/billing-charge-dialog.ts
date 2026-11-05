import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, min, required } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addBillingBatchCharge,
  Api,
  type BillingBatchDto,
  type BillingChargeKind,
} from "@trackwise/shared/api";
import { billingChargeKindOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  Stack,
  UiButton,
  UiCheckboxField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";

interface ChargeModel {
  kind: BillingChargeKind;
  description: string;
  amount: number | null;
  isVatable: boolean;
  tripId: string | null;
}

/** A charge on top of the trip rates: demurrage, a backload, a different location, tolls at cost. */
@Component({
  selector: "app-billing-charge-dialog",
  templateUrl: "./billing-charge-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Stack,
    UiButton,
    UiCheckboxField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class BillingChargeDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly batch = input.required<BillingBatchDto>();
  public readonly open = model(false);
  public readonly saved = output<BillingBatchDto>();

  protected readonly kindOptions = billingChargeKindOptions;
  protected readonly tripOptions = computed(() =>
    (this.batch().trips ?? []).map((t) => ({
      label: `Trip ${t.tripNumber} · ${t.billingDate} · ${t.truckNumber ?? ""}`,
      value: t.tripId!,
    })),
  );

  protected readonly model = signal<ChargeModel>(emptyModel());

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.kind, { message: "Pick what the charge is for." });
      required(p.description, { message: "Describe the charge." });
      maxLength(p.description, 200, { message: "Keep it under 200 characters." });
      required(p.amount, { message: "Enter the amount." });
      min(p.amount, 0.01, { message: "The amount must be more than zero." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          try {
            const batch = await this.api.invoke(addBillingBatchCharge, {
              id: this.batch().id!,
              body: {
                kind: v.kind,
                description: v.description.trim(),
                amount: v.amount!,
                isVatable: v.isVatable,
                tripId: v.tripId,
              },
            });
            this.toast.showSuccess("Charge added.");
            this.open.set(false);
            this.saved.emit(batch);
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not add the charge."));
          }
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    this.model.set(emptyModel());
  }

  /** Charges billed at cost aren't VATable; everything else is. */
  protected onKindChange(kind: BillingChargeKind | null): void {
    this.model.update((m) => ({ ...m, isVatable: kind !== "tolls_and_parking" }));
  }
}

function emptyModel(): ChargeModel {
  return { kind: "demurrage", description: "", amount: null, isVatable: true, tripId: null };
}
