import { Component, inject, model, signal } from "@angular/core";
import { form, FormField, FormRoot, validate } from "@angular/forms/signals";
import { ToastService } from "@trackwise/shared";
import {
  Grid,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { TripDetailsStore } from "../../store/trip-details.store";

interface GarageTimesModel {
  departedAt: Date | null;
  arrivedAt: Date | null;
}

/** Records when the truck left the garage for the trip and when it got back, as reported. */
@Component({
  selector: "app-garage-times-dialog",
  templateUrl: "./garage-times-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    ValidatedForm,
  ],
})
export class GarageTimesDialog {
  private readonly store = inject(TripDetailsStore);
  private readonly toastService = inject(ToastService);

  public readonly visible = model(false);

  protected readonly model = signal<GarageTimesModel>({ departedAt: null, arrivedAt: null });

  protected readonly form = form(
    this.model,
    (p) => {
      validate(p.departedAt, ({ valueOf }) =>
        valueOf(p.departedAt) || valueOf(p.arrivedAt)
          ? null
          : { kind: "empty", message: "Enter at least one garage time." },
      );
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const saved = await this.store.recordGarageTimes(
            v.departedAt?.toISOString() ?? null,
            v.arrivedAt?.toISOString() ?? null,
          );
          if (saved) {
            this.visible.set(false);
          } else {
            this.toastService.showError(this.store.error() ?? "Couldn't save the garage times.");
          }
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    const trip = this.store.trip();
    this.model.set({
      departedAt: trip?.garageDepartedAt ? new Date(trip.garageDepartedAt) : null,
      arrivedAt: trip?.garageArrivedAt ? new Date(trip.garageArrivedAt) : null,
    });
    this.form().reset();
  }
}
