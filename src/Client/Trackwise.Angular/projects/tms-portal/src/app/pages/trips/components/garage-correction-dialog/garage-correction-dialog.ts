import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import {
  form,
  FormField,
  FormRoot,
  maxLength,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import { Api, correctTripGarageTimes, type TripDto } from "@trackwise/shared/api";
import {
  Grid,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import {
  describeTime,
  resolveChain,
  toClockText,
  type ResolvedTime,
} from "../../utils/report-times";

interface GarageModel {
  day: Date | null;
  garageOut: string;
  garageIn: string;
  reason: string;
}

/**
 * Corrects the recorded garage departure and arrival. Typed like the report (0530) and dated forward
 * from the departure day; a trip that ran past a second midnight takes a trailing "+".
 */
@Component({
  selector: "app-garage-correction-dialog",
  templateUrl: "./garage-correction-dialog.html",
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
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class GarageCorrectionDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  public readonly open = model(false);
  public readonly done = output<void>();

  protected readonly model = signal<GarageModel>({
    day: null,
    garageOut: "",
    garageIn: "",
    reason: "",
  });

  /** Only a recorded time is corrected; one not yet recorded is entered on the report. */
  protected readonly hasOut = computed(() => !!this.trip().garageDepartedAt);
  protected readonly hasIn = computed(() => !!this.trip().garageArrivedAt);

  private readonly resolved = computed(() => {
    const m = this.model();
    if (!m.day) return new Map<string, ResolvedTime>();
    return resolveChain(m.day, [
      { key: "out", text: m.garageOut },
      { key: "in", text: m.garageIn },
    ]);
  });

  protected readonly form = form(
    this.model,
    (p) => {
      const badTime = (key: string) => () =>
        this.resolved().get(key) === "invalid"
          ? { kind: "badTime", message: "Type a time like 0530 or 05:30." }
          : null;

      required(p.day, { message: "Pick the day." });
      readonlyWhen(p.garageOut, { when: () => !this.hasOut() });
      readonlyWhen(p.garageIn, { when: () => !this.hasIn() });
      validate(p.garageOut, badTime("out"));
      validate(p.garageIn, badTime("in"));
      required(p.garageOut, {
        message: "A recorded time can be corrected, not cleared.",
        when: () => this.hasOut(),
      });
      required(p.garageIn, {
        message: "A recorded time can be corrected, not cleared.",
        when: () => this.hasIn(),
      });
      required(p.reason, { message: "Say why the recorded times are being corrected." });
      maxLength(p.reason, 500, { message: "Keep it under 500 characters." });
    },
    {
      submission: {
        action: async () => {
          const iso = (key: string, recorded: boolean): string | null => {
            const at = this.resolved().get(key);
            return recorded && at instanceof Date ? at.toISOString() : null;
          };

          try {
            await this.api.invoke(correctTripGarageTimes, {
              tripId: this.trip().id!,
              body: {
                departedAt: iso("out", this.hasOut()),
                arrivedAt: iso("in", this.hasIn()),
                reason: this.model().reason.trim(),
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

  protected hint(key: "out" | "in"): string {
    const at = this.resolved().get(key);
    return at instanceof Date ? describeTime(at) : "";
  }

  protected onOpened(): void {
    const trip = this.trip();
    const first = new Date(trip.garageDepartedAt ?? trip.garageArrivedAt!);
    this.form().reset({
      day: new Date(first.getFullYear(), first.getMonth(), first.getDate()),
      garageOut: toClockText(trip.garageDepartedAt),
      garageIn: toClockText(trip.garageArrivedAt),
      reason: "",
    });
  }
}
