import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, validate } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  recordTripRescueGarageTimes,
  type TripDto,
  type TripRescueDto,
} from "@trackwise/shared/api";
import {
  Grid,
  Typography,
  UiButton,
  UiDialog,
  UiFormField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { describeTime, parseClock, resolveChain, toClockText } from "../../utils/report-times";

interface GarageModel {
  departed: string;
  arrived: string;
}

/**
 * When the rescue truck left its own garage and got back - its crew's hours, apart from the trip's
 * own truck. Typed as the crew report them (hhmm); the day follows the transfer.
 */
@Component({
  selector: "app-rescue-garage-dialog",
  templateUrl: "./rescue-garage-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    Typography,
    UiButton,
    UiDialog,
    UiFormField,
    UiTextField,
    ValidatedForm,
  ],
})
export class RescueGarageDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  /** Null until the card picks one; the dialog stays mounted so opening it fires `opened`. */
  public readonly rescue = input<TripRescueDto | null>(null);
  public readonly open = model(false);
  public readonly done = output<void>();

  protected readonly model = signal<GarageModel>({ departed: "", arrived: "" });

  /** The transfer, then the rescued drops' recorded times - what the garage times bracket. */
  private readonly transfer = computed(() => new Date(this.rescue()?.transferredAt ?? Date.now()));
  private readonly lastRescued = computed(() => {
    const ids = new Set(this.rescue()?.stopIds ?? []);
    const times = (this.trip().stops ?? [])
      .filter((s) => ids.has(s.id!))
      .flatMap((s) => [s.arrivedAt, s.departedAt])
      .filter((t): t is string => !!t)
      .map((t) => new Date(t).getTime());
    return new Date(Math.max(this.transfer().getTime(), ...times));
  });

  protected readonly departedAt = computed(() => this.departure(this.model().departed));
  protected readonly arrivedAt = computed(() => this.arrival(this.model().arrived));

  protected readonly form = form(
    this.model,
    (p) => {
      validate(p.departed, ({ value }) =>
        parseClock(value()) === "invalid"
          ? { kind: "badTime", message: "Type a time like 0930 or 09:30." }
          : null,
      );
      validate(p.arrived, ({ value }) =>
        parseClock(value()) === "invalid"
          ? { kind: "badTime", message: "Type a time like 1930 or 19:30." }
          : null,
      );
    },
    {
      submission: {
        action: async () => {
          const rescue = this.rescue();
          if (!rescue) return undefined;
          try {
            await this.api.invoke(recordTripRescueGarageTimes, {
              tripId: this.trip().id!,
              rescueId: rescue.id!,
              body: {
                departedAt: this.departedAt()?.toISOString() ?? null,
                arrivedAt: this.arrivedAt()?.toISOString() ?? null,
              },
            });
            this.toast.showSuccess("Rescue truck's garage times saved");
            this.open.set(false);
            this.done.emit();
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't save the garage times."));
          }
          return undefined;
        },
      },
    },
  );

  protected readonly describe = describeTime;

  protected onOpened(): void {
    this.form().reset({
      departed: toClockText(this.rescue()?.garageDepartedAt),
      arrived: toClockText(this.rescue()?.garageArrivedAt),
    });
  }

  /** On the transfer's day, or the evening before when that time is later than the transfer. */
  private departure(text: string): Date | null {
    const clock = parseClock(text);
    if (!clock || clock === "invalid") return null;
    const t = this.transfer();
    const at = new Date(t.getFullYear(), t.getMonth(), t.getDate(), 0, clock.minutes);
    if (at > t) at.setDate(at.getDate() - 1);
    return at;
  }

  /** The first such time after the last rescued drop - an overnight return lands on the next day. */
  private arrival(text: string): Date | null {
    if (!text.trim()) return null;
    const anchor = this.lastRescued();
    const resolved = resolveChain(anchor, [
      { key: "anchor", text: "", fixed: anchor },
      { key: "arrived", text },
    ]).get("arrived");
    return resolved instanceof Date ? resolved : null;
  }
}
