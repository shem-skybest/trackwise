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
  correctTripStop,
  type DropReturnReason,
  type LoadStatus,
} from "@trackwise/shared/api";
import { dropOutcomeOptions, dropReturnReasonOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import type { BlockInfo } from "../../trip-report/trip-report.model";
import {
  describeTime,
  resolveChain,
  toClockText,
  type ResolvedTime,
} from "../../utils/report-times";

type TimeKey = "arrival" | "handlingStart" | "handlingFinish" | "departure";

interface CorrectionModel {
  day: Date | null;
  arrival: string;
  tempArrival: number | null;
  handlingStart: string;
  handlingFinish: string;
  departure: string;
  tempDeparture: number | null;
  outcome: LoadStatus | null;
  returnedBoxes: number | null;
  returnedBags: number | null;
  returnReason: DropReturnReason | null;
  note: string;
  reason: string;
}

const EMPTY: CorrectionModel = {
  day: null,
  arrival: "",
  tempArrival: null,
  handlingStart: "",
  handlingFinish: "",
  departure: "",
  tempDeparture: null,
  outcome: null,
  returnedBoxes: null,
  returnedBags: null,
  returnReason: null,
  note: "",
  reason: "",
};

/**
 * Corrects what's already recorded on one block of the crew report - a warehouse visit or a drop.
 * Times are typed like the report (2329) and dated forward from the arrival day. A reason is
 * required; every changed value is kept in the trip's correction history.
 */
@Component({
  selector: "app-stop-correction-dialog",
  templateUrl: "./stop-correction-dialog.html",
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
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class StopCorrectionDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly tripId = input.required<string>();
  /** The block being corrected; the dialog stays empty until one is picked. */
  public readonly info = input<BlockInfo | null>(null);
  public readonly open = model(false);
  public readonly done = output<void>();

  protected readonly outcomeOptions = dropOutcomeOptions;
  protected readonly reasonOptions = dropReturnReasonOptions;

  protected readonly model = signal<CorrectionModel>(EMPTY);

  protected readonly isDrop = computed(() => this.info()?.kind === "drop");
  /** Only a recorded departure is corrected; one not yet recorded is entered on the report. */
  protected readonly hasDeparture = computed(() => !!this.info()?.stop.departedAt);
  protected readonly handlingLabel = computed(() => (this.isDrop() ? "unloading" : "loading"));
  protected readonly header = computed(
    () => `Correct ${this.info()?.title.toLowerCase() ?? "stop"}`,
  );

  private readonly resolved = computed(() => {
    const m = this.model();
    if (!m.day) return new Map<string, ResolvedTime>();
    return resolveChain(m.day, [
      { key: "arrival", text: m.arrival },
      { key: "handlingStart", text: m.handlingStart },
      { key: "handlingFinish", text: m.handlingFinish },
      { key: "departure", text: m.departure },
    ]);
  });

  protected readonly form = form(
    this.model,
    (p) => {
      const badTime = (key: TimeKey) => () =>
        this.resolved().get(key) === "invalid"
          ? { kind: "badTime", message: "Type a time like 2329 or 23:29." }
          : null;
      const correctsDrop = () => this.hasDeparture() && this.isDrop();

      required(p.day, { message: "Pick the arrival day." });
      required(p.arrival, { message: "A recorded arrival can be corrected, not cleared." });
      validate(p.arrival, badTime("arrival"));
      validate(p.handlingStart, badTime("handlingStart"));
      validate(p.handlingFinish, badTime("handlingFinish"));
      validate(p.departure, badTime("departure"));
      required(p.departure, {
        message: "A recorded departure can be corrected, not cleared.",
        when: () => this.hasDeparture(),
      });
      required(p.outcome, { message: "Pick the drop's outcome.", when: correctsDrop });
      min(p.returnedBoxes, 0, { message: "Can't be negative." });
      min(p.returnedBags, 0, { message: "Can't be negative." });
      validate(p.returnReason, ({ value, valueOf }) =>
        correctsDrop() &&
        ((valueOf(p.returnedBoxes) ?? 0) > 0 || (valueOf(p.returnedBags) ?? 0) > 0) &&
        !value()
          ? { kind: "reasonRequired", message: "Say why they came back." }
          : null,
      );
      validate(p.note, ({ value, valueOf }) =>
        correctsDrop() &&
        (valueOf(p.outcome) === "foul_trip" || valueOf(p.outcome) === "cancelled_by_client") &&
        !value().trim()
          ? { kind: "noteRequired", message: "Say what happened." }
          : null,
      );
      required(p.reason, { message: "Say why the recorded values are being corrected." });
      maxLength(p.reason, 500, { message: "Keep it under 500 characters." });
    },
    {
      submission: {
        action: async () => {
          const info = this.info();
          if (!info) return undefined;

          const m = this.model();
          const iso = (key: TimeKey): string | null => {
            const at = this.resolved().get(key);
            return at instanceof Date ? at.toISOString() : null;
          };
          const departure = this.hasDeparture();
          const drop = departure && this.isDrop();

          try {
            await this.api.invoke(correctTripStop, {
              tripId: this.tripId(),
              stopId: info.key,
              body: {
                arrivedAt: iso("arrival")!,
                temperatureAtArrival: m.tempArrival,
                handlingStartedAt: departure ? iso("handlingStart") : null,
                handlingFinishedAt: departure ? iso("handlingFinish") : null,
                departedAt: departure ? iso("departure") : null,
                temperatureAtDeparture: departure ? m.tempDeparture : null,
                outcome: drop ? (m.outcome ?? undefined) : undefined,
                returnedBoxes: drop ? m.returnedBoxes : null,
                returnedBags: drop ? m.returnedBags : null,
                returnReason: drop ? (m.returnReason ?? undefined) : undefined,
                outcomeNote: drop ? m.note.trim() || null : null,
                reason: m.reason.trim(),
              },
            });
            this.toast.showSuccess("Correction saved.");
            this.open.set(false);
            this.done.emit();
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't save the correction."));
          }
          return undefined;
        },
      },
    },
  );

  protected hint(key: TimeKey): string {
    const at = this.resolved().get(key);
    return at instanceof Date ? describeTime(at) : "";
  }

  protected onOpened(): void {
    const info = this.info();
    if (!info) return;

    const { stop, load } = info;
    const arrivedAt = new Date(stop.arrivedAt!);
    this.form().reset({
      day: new Date(arrivedAt.getFullYear(), arrivedAt.getMonth(), arrivedAt.getDate()),
      arrival: toClockText(stop.arrivedAt),
      tempArrival: stop.temperatureAtArrival ?? null,
      handlingStart: toClockText(stop.handlingStartedAt),
      handlingFinish: toClockText(stop.handlingFinishedAt),
      departure: toClockText(stop.departedAt),
      tempDeparture: stop.temperatureAtDeparture ?? null,
      outcome: info.kind === "drop" && stop.departedAt ? (load?.status ?? null) : null,
      returnedBoxes: load?.returnedBoxes ?? null,
      returnedBags: load?.returnedBags ?? null,
      returnReason: load?.returnReason ?? null,
      note: load?.outcomeNote ?? "",
      reason: "",
    });
  }
}
