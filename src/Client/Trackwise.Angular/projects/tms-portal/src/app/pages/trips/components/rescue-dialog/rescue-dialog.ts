import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, maxLength, required, validate } from "@angular/forms/signals";
import { DRIVING_ROLES, getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  recordTripRescue,
  type EmployeeDto,
  type HelperDto,
  type IncidentDto,
  type TripDto,
  type TruckDto,
} from "@trackwise/shared/api";
import {
  Grid,
  Typography,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiMultiSelectField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { incidentKindLabel } from "@/pages/incidents/utils/incident-labels";
import { SearchEmployee, SearchHelper, SearchTruck } from "@/shared/components/search";
import { parseClock } from "../../utils/report-times";
import { dropNumber, rescuableDrops } from "../../utils/rescue";
import { crewDay } from "../../utils/trip-header";

interface RescueModel {
  truck: TruckDto | null;
  driver: EmployeeDto | null;
  helper: HelperDto | null;
  secondHelper: HelperDto | null;
  stopIds: string[];
  day: Date | null;
  time: string;
  place: string;
  reason: string;
  incidentId: string | null;
}

/**
 * Records another truck taking over the drops the trip's own truck can't finish - Cold Chain's
 * rescue unit. The goods move across, usually with the trip's helpers, so they are filled in; the
 * rescue truck brings its own driver. The trip keeps its truck and price.
 */
@Component({
  selector: "app-rescue-dialog",
  templateUrl: "./rescue-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    SearchEmployee,
    SearchHelper,
    SearchTruck,
    Typography,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiMultiSelectField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class RescueDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  /** Incidents already on record for this trip; the rescue can name the one that caused it. */
  public readonly incidents = input<readonly IncidentDto[]>([]);
  public readonly open = model(false);
  public readonly done = output<void>();

  protected readonly drivingRoles = DRIVING_ROLES;
  protected readonly crewDay = computed(() => crewDay(this.trip()));

  protected readonly dropOptions = computed(() => {
    const trip = this.trip();
    const loads = new Map((trip.loads ?? []).map((l) => [l.id, l] as const));
    return rescuableDrops(trip).map((s) => ({
      value: s.id!,
      label: [
        `Drop ${dropNumber(trip, s.id)}`,
        loads.get(s.loadId)?.customer?.name,
        s.address?.cityOrMunicipality,
      ]
        .filter(Boolean)
        .join(" · "),
    }));
  });

  protected readonly incidentOptions = computed(() =>
    this.incidents().map((i) => ({
      value: i.id!,
      label: `${i.caseNumber} ${incidentKindLabel(i.kind)} - ${i.summary?.slice(0, 60) ?? ""}`,
    })),
  );

  protected readonly model = signal<RescueModel>({
    truck: null,
    driver: null,
    helper: null,
    secondHelper: null,
    stopIds: [],
    day: null,
    time: "",
    place: "",
    reason: "",
    incidentId: null,
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.truck, { message: "Pick the rescue truck." });
      validate(p.truck, ({ value }) =>
        value()?.id && value()!.id === this.trip().truckId
          ? { kind: "sameTruck", message: "Pick another truck than the trip's own." }
          : null,
      );
      required(p.driver, { message: "Pick the rescue truck's driver." });
      validate(p.stopIds, ({ value }) =>
        value().length === 0
          ? { kind: "required", message: "Pick the drops the rescue truck took over." }
          : null,
      );
      required(p.day, { message: "Pick the day of the transfer." });
      validate(p.time, ({ value }) => {
        const clock = parseClock(value());
        if (clock === null)
          return { kind: "required", message: "Enter when the goods moved across." };
        return clock === "invalid"
          ? { kind: "badTime", message: "Type a time like 1313 or 13:13." }
          : null;
      });
      required(p.reason, { message: "Say why the trip needed a rescue." });
      maxLength(p.reason, 500, { message: "Keep the reason under 500 characters." });
      maxLength(p.place, 500, { message: "Keep the place under 500 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          try {
            await this.api.invoke(recordTripRescue, {
              tripId: this.trip().id!,
              body: {
                truckId: v.truck!.id!,
                driverId: v.driver!.id!,
                helperId: v.helper?.id ?? null,
                secondHelperId: v.secondHelper?.id ?? null,
                stopIds: v.stopIds,
                transferredAt: transferTime(v.day!, v.time).toISOString(),
                place: v.place.trim() || null,
                reason: v.reason.trim(),
                incidentId: v.incidentId,
              },
            });
            this.toast.showSuccess("Rescue recorded");
            this.open.set(false);
            this.done.emit();
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't record the rescue."));
          }
          return undefined;
        },
      },
    },
  );

  protected onOpened(): void {
    this.form().reset(this.startingModel());
  }

  /** The trip's helpers move with the goods; every drop still open is taken over unless unticked. */
  private startingModel(): RescueModel {
    const trip = this.trip();
    const helper = (id?: string | null, name?: string | null): HelperDto | null =>
      id ? ({ id, name: name ?? "" } as HelperDto) : null;
    return {
      truck: null,
      driver: null,
      helper: helper(trip.helperId, trip.helperName),
      secondHelper: helper(trip.secondHelperId, trip.secondHelperName),
      stopIds: rescuableDrops(trip).map((s) => s.id!),
      day: lastEventDay(trip),
      time: "",
      place: "",
      reason: "",
      incidentId: this.incidents().length === 1 ? this.incidents()[0].id! : null,
    };
  }
}

/** The transfer happens after the last event on record, so its day is where to start. */
function lastEventDay(trip: TripDto): Date {
  const times = (trip.stops ?? [])
    .flatMap((s) => [s.arrivedAt, s.departedAt])
    .concat(trip.garageDepartedAt)
    .filter((t): t is string => !!t)
    .map((t) => new Date(t).getTime());
  const at = times.length ? new Date(Math.max(...times)) : new Date();
  return new Date(at.getFullYear(), at.getMonth(), at.getDate());
}

function transferTime(day: Date, text: string): Date {
  const clock = parseClock(text);
  const minutes = clock && clock !== "invalid" ? clock.minutes : 0;
  const extra = clock && clock !== "invalid" ? clock.extraDays : 0;
  return new Date(
    day.getFullYear(),
    day.getMonth(),
    day.getDate() + extra,
    Math.floor(minutes / 60),
    minutes % 60,
  );
}
