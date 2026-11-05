import { Component, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import { Api, updateWorkfileHeader, type WorkfileDto } from "@trackwise/shared/api";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { dateToIso, isoToDate } from "@/shared/utils";

interface ShipmentModel {
  billOfLadingNumber: string;
  vesselName: string;
  voyageNumber: string;
  origin: string;
  destination: string;
  commodity: string;
  etd: Date | null;
  eta: Date | null;
}

/**
 * Edits a workfile's shipment details - B/L, vessel, ports and the ETD/ETA the door-to-door
 * timeline reads. The header update is a patch, so the parties and commercial terms are left alone.
 */
@Component({
  selector: "app-workfile-shipment-dialog",
  templateUrl: "./workfile-shipment-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiTextField,
    ValidatedForm,
  ],
})
export class WorkfileShipmentDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly workfile = input.required<WorkfileDto>();
  public readonly open = model(false);
  public readonly saved = output();

  protected readonly model = signal<ShipmentModel>({
    billOfLadingNumber: "",
    vesselName: "",
    voyageNumber: "",
    origin: "",
    destination: "",
    commodity: "",
    etd: null,
    eta: null,
  });

  protected readonly form = form(this.model, () => undefined, {
    submission: {
      action: async () => {
        const v = this.model();
        try {
          await this.api.invoke(updateWorkfileHeader, {
            id: this.workfile().id!,
            body: {
              billOfLadingNumber: v.billOfLadingNumber.trim() || null,
              vesselName: v.vesselName.trim() || null,
              voyageNumber: v.voyageNumber.trim() || null,
              origin: v.origin.trim() || null,
              destination: v.destination.trim() || null,
              commodity: v.commodity.trim() || null,
              etd: dateToIso(v.etd),
              eta: dateToIso(v.eta),
            },
          });
          this.toast.showSuccess("Shipment details saved.");
          this.open.set(false);
          this.saved.emit();
        } catch (error) {
          this.toast.showError(getApiErrorMessage(error, "Could not save the shipment details."));
        }
        return undefined;
      },
    },
  });

  protected onOpened(): void {
    this.model.set(this.fromWorkfile());
  }

  private fromWorkfile(): ShipmentModel {
    const w = this.workfile();
    return {
      billOfLadingNumber: w.billOfLadingNumber ?? "",
      vesselName: w.vesselName ?? "",
      voyageNumber: w.voyageNumber ?? "",
      origin: w.origin ?? "",
      destination: w.destination ?? "",
      commodity: w.commodity ?? "",
      etd: isoToDate(w.etd),
      eta: isoToDate(w.eta),
    };
  }
}
