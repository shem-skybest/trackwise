import { Component, computed, inject, input, model, signal } from "@angular/core";
import { form, FormField, FormRoot, min, required, validate } from "@angular/forms/signals";
import { ToastService } from "@trackwise/shared";
import type { DropReturnReason, LoadStatus, TripLoadDto, TripStopDto } from "@trackwise/shared/api";
import { dropOutcomeOptions, dropReturnReasonOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  Stack,
  Typography,
  UiButton,
  UiCheckboxField,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { TripDetailsStore } from "../../store/trip-details.store";

export type StopEventMode = "arrive" | "depart";

interface StopEventModel {
  at: Date | null;
  temperature: number | null;
  handlingStartedAt: Date | null;
  handlingFinishedAt: Date | null;
  outcome: LoadStatus | null;
  returnedBoxes: number | null;
  returnedBags: number | null;
  returnReason: DropReturnReason | null;
  note: string;
  includeSameVisit: boolean;
}

/**
 * Records one stop event the way the office receives it - a crew member's message typed in after
 * the fact. Every time is the reported time, defaulting to now. A drop's departure carries its
 * outcome and any boxes or bags that came back; a pickup's events can cover the whole warehouse
 * visit (every pickup at the same place) in one entry.
 */
@Component({
  selector: "app-stop-event-dialog",
  templateUrl: "./stop-event-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Stack,
    Typography,
    UiButton,
    UiCheckboxField,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    ValidatedForm,
  ],
})
export class StopEventDialog {
  private readonly store = inject(TripDetailsStore);
  private readonly toastService = inject(ToastService);

  public readonly visible = model(false);
  public readonly mode = input<StopEventMode>("arrive");
  public readonly stop = input<TripStopDto | null>(null);
  public readonly load = input<TripLoadDto | null>(null);
  /** How many pickups share this stop's warehouse visit, the stop itself included. */
  public readonly visitSize = input(1);

  protected readonly outcomeOptions = dropOutcomeOptions;
  protected readonly reasonOptions = dropReturnReasonOptions;

  protected readonly isDrop = computed(() => this.stop()?.type === "drop_off");
  protected readonly isDeparture = computed(() => this.mode() === "depart");
  protected readonly header = computed(() => {
    const where = this.isDrop() ? "drop" : "pickup";
    return this.isDeparture() ? `Record ${where} departure` : `Record ${where} arrival`;
  });
  protected readonly handlingLabel = computed(() => (this.isDrop() ? "Unloading" : "Loading"));

  protected readonly model = signal<StopEventModel>(this.emptyModel());

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.at, { message: "Enter when it happened." });
      required(p.outcome, {
        when: () => this.isDeparture() && this.isDrop(),
        message: "Choose how the drop went.",
      });
      required(p.note, {
        when: ({ valueOf }) =>
          valueOf(p.outcome) === "foul_trip" || valueOf(p.outcome) === "cancelled_by_client",
        message: "Say what happened - a foul trip or cancellation needs a reason.",
      });
      required(p.returnReason, {
        when: ({ valueOf }) =>
          (valueOf(p.returnedBoxes) ?? 0) > 0 || (valueOf(p.returnedBags) ?? 0) > 0,
        message: "Say why the boxes or bags came back.",
      });
      min(p.returnedBoxes, 0, { message: "Can't be negative." });
      min(p.returnedBags, 0, { message: "Can't be negative." });
      validate(p.returnedBoxes, ({ value }) => {
        const planned = this.load()?.plannedBoxes;
        return planned != null && (value() ?? 0) > planned
          ? { kind: "overPlanned", message: `Only ${planned} boxes were planned.` }
          : null;
      });
      validate(p.returnedBags, ({ value }) => {
        const planned = this.load()?.plannedBags;
        return planned != null && (value() ?? 0) > planned
          ? { kind: "overPlanned", message: `Only ${planned} bags were planned.` }
          : null;
      });
    },
    {
      submission: {
        action: async () => {
          const saved = this.isDeparture() ? await this.saveDeparture() : await this.saveArrival();
          if (saved) {
            this.visible.set(false);
          } else {
            this.toastService.showError(this.store.error() ?? "Couldn't save the stop.");
          }
          return undefined;
        },
      },
    },
  );

  protected readonly isPartialOutcome = computed(() => this.model().outcome === "delivered");

  protected onOpened(): void {
    this.model.set(this.emptyModel());
    this.form().reset();
  }

  private async saveArrival(): Promise<boolean> {
    const stop = this.stop();
    const v = this.model();
    if (!stop?.id) return false;
    return this.store.markStopArrived(stop.id, {
      arrivedAt: v.at?.toISOString() ?? null,
      temperatureAtArrival: v.temperature,
      includeSameVisitPickups: !this.isDrop() && v.includeSameVisit,
    });
  }

  private async saveDeparture(): Promise<boolean> {
    const stop = this.stop();
    const v = this.model();
    if (!stop?.id) return false;
    const partial = this.isDrop() && v.outcome === "delivered";
    return this.store.markStopDeparted(stop.id, {
      departedAt: v.at?.toISOString() ?? null,
      temperatureAtDeparture: v.temperature,
      handlingStartedAt: v.handlingStartedAt?.toISOString() ?? null,
      handlingFinishedAt: v.handlingFinishedAt?.toISOString() ?? null,
      outcome: this.isDrop() ? (v.outcome ?? undefined) : undefined,
      returnedBoxes: partial ? v.returnedBoxes : null,
      returnedBags: partial ? v.returnedBags : null,
      returnReason: partial && v.returnReason ? v.returnReason : undefined,
      outcomeNote: v.note.trim() || null,
      includeSameVisitPickups: !this.isDrop() && v.includeSameVisit,
    });
  }

  private emptyModel(): StopEventModel {
    return {
      at: new Date(),
      temperature: null,
      handlingStartedAt: null,
      handlingFinishedAt: null,
      outcome: "delivered",
      returnedBoxes: null,
      returnedBags: null,
      returnReason: null,
      note: "",
      includeSameVisit: true,
    };
  }
}
