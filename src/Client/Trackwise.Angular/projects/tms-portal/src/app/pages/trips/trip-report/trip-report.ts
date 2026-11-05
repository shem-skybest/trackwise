import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import {
  applyEach,
  form,
  FormField,
  FormRoot,
  min,
  readonly as readonlyWhen,
  required,
  validate,
} from "@angular/forms/signals";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  getIncidents,
  getTripById,
  getTripCorrections,
  recordTripReport,
  updateTrip,
  type IncidentDto,
  type TripCorrectionDto,
  type TripDto,
  type TripEventSource,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import {
  Grid,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiFormField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { PageHeader } from "@/shared/components";
import { GarageCorrectionDialog } from "../components/garage-correction-dialog/garage-correction-dialog";
import { ReportHeader } from "../components/report-header/report-header";
import { ReportRescues } from "../components/report-rescues/report-rescues";
import { ReportStopBlock } from "../components/report-stop-block/report-stop-block";
import { StopCorrectionDialog } from "../components/stop-correction-dialog/stop-correction-dialog";
import { TripPhotosStore } from "../store/trip-photos.store";
import { describeChange, groupCorrections } from "../utils/corrections";
import { describeTime, resolveChain } from "../utils/report-times";
import { rescueLabel } from "../utils/rescue";
import { canRemoveDrop, headerUpdate } from "../utils/trip-header";
import {
  buildBlocks,
  buildCommand,
  buildModel,
  chainEntries,
  GARAGE_IN,
  GARAGE_OUT,
  timeKey,
  type BlockInfo,
  type ReportModel,
} from "./trip-report.model";

/** A reported time this far past the clock is a mistyped day, not a fast phone. */
const FUTURE_SKEW_MS = 5 * 60 * 1000;

const EMPTY: ReportModel = {
  startDay: null,
  garageOutRecorded: false,
  garageInRecorded: false,
  garageOut: "",
  garageIn: "",
  blocks: [],
};

/**
 * One trip's crew report, laid out like the driver's Messenger message so the office can retype it
 * top to bottom: header, garage departure, the warehouse, each drop, garage arrival. Times are typed
 * as the crew write them (2329, 0323H) and dated by the order of events. Enter moves to the next
 * field; Ctrl+S saves. Everything saves in one go or not at all.
 */
@Component({
  selector: "app-trip-report",
  templateUrl: "./trip-report.html",
  host: { "(keydown)": "onKeydown($event)" },
  providers: [TripPhotosStore],
  imports: [
    DatePipe,
    FormField,
    FormRoot,
    GarageCorrectionDialog,
    Grid,
    PageHeader,
    ReportHeader,
    ReportRescues,
    ReportStopBlock,
    RouterLink,
    Spinner,
    Stack,
    StopCorrectionDialog,
    Typography,
    UiButton,
    UiDateField,
    UiFormField,
    UiTextField,
    ValidatedForm,
  ],
})
export class TripReportPage {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  protected readonly tripId = inject(ActivatedRoute).snapshot.paramMap.get("tripId")!;

  protected readonly trip = signal<TripDto | null>(null);
  protected readonly blocks = signal<BlockInfo[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly model = signal<ReportModel>(EMPTY);
  protected readonly corrections = signal<TripCorrectionDto[]>([]);
  protected readonly incidents = signal<IncidentDto[]>([]);
  protected readonly correcting = signal<BlockInfo | null>(null);
  protected readonly stopCorrectionOpen = signal(false);
  protected readonly garageCorrectionOpen = signal(false);
  protected readonly describeChange = describeChange;

  /** Load ids of the drops that can still be taken off the trip. */
  protected readonly removableDrops = computed(() => {
    const trip = this.trip();
    const drops = this.blocks().filter((b) => b.kind === "drop");
    return new Set(
      drops.map((b) => b.load?.id).filter((id): id is string => !!trip && canRemoveDrop(trip, id)),
    );
  });

  /** Each block's correction history, by the block's first stop - one row per stop changed. */
  protected readonly blockCorrections = computed(() => {
    const byStop = new Map<string, TripCorrectionDto[]>();
    for (const c of this.corrections()) {
      if (!c.stopId) continue;
      byStop.set(c.stopId, [...(byStop.get(c.stopId) ?? []), c]);
    }
    return new Map(
      this.blocks().map((b) => [b.key, groupCorrections(byStop.get(b.key) ?? [])] as const),
    );
  });

  /** "ABF3727 · driver" for each drop a rescue took over, by the block's stop. */
  protected readonly rescuedBy = computed(() => {
    const trip = this.trip();
    const rescues = new Map((trip?.rescues ?? []).map((r) => [r.id, r] as const));
    const byStop = new Map<string, string>();
    for (const stop of trip?.stops ?? []) {
      const rescue = stop.rescueId ? rescues.get(stop.rescueId) : undefined;
      if (rescue) byStop.set(stop.id!, rescueLabel(rescue));
    }
    return byStop;
  });

  protected readonly garageCorrections = computed(() =>
    groupCorrections(this.corrections().filter((c) => !c.stopId)),
  );

  protected readonly resolved = computed(() => {
    const trip = this.trip();
    const model = this.model();
    return trip && model.startDay
      ? resolveChain(model.startDay, chainEntries(trip, this.blocks(), model))
      : new Map<string, Date | "invalid">();
  });

  protected readonly hints = computed(() => {
    const hints = new Map<string, string>();
    for (const [key, at] of this.resolved()) {
      if (at instanceof Date) hints.set(key, describeTime(at));
    }
    return hints;
  });

  protected readonly garageOutHint = computed(() =>
    withSource(this.hints().get(GARAGE_OUT), this.trip()?.garageDepartureSource),
  );
  protected readonly garageInHint = computed(() =>
    withSource(this.hints().get(GARAGE_IN), this.trip()?.garageArrivalSource),
  );

  protected readonly dropCount = computed(
    () => this.blocks().filter((b) => b.kind === "drop").length,
  );

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.startDay, {
        message: "Pick the pre-load day - the report's first time falls on it.",
      });
      readonlyWhen(p.garageOut, { when: ({ valueOf }) => valueOf(p.garageOutRecorded) });
      readonlyWhen(p.garageIn, { when: ({ valueOf }) => valueOf(p.garageInRecorded) });
      validate(p.garageOut, () => this.timeProblem(GARAGE_OUT));
      validate(p.garageIn, () => this.timeProblem(GARAGE_IN));

      applyEach(p.blocks, (b) => {
        // Recorded events can't be retyped here - the API keeps the first entry.
        readonlyWhen(b.arrival, { when: ({ valueOf }) => valueOf(b.arrivalRecorded) });
        readonlyWhen(b.tempArrival, { when: ({ valueOf }) => valueOf(b.arrivalRecorded) });
        readonlyWhen(b.handlingStart, {
          when: ({ valueOf }) => valueOf(b.departureRecorded) || valueOf(b.handlingStartRecorded),
        });
        readonlyWhen(b.handlingFinish, {
          when: ({ valueOf }) => valueOf(b.departureRecorded) || valueOf(b.handlingFinishRecorded),
        });
        readonlyWhen(b.departure, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.tempDeparture, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.outcome, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.returnedBoxes, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.returnedBags, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.returnReason, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        readonlyWhen(b.note, { when: ({ valueOf }) => valueOf(b.departureRecorded) });
        min(b.returnedBoxes, 0, { message: "Can't be negative." });
        min(b.returnedBags, 0, { message: "Can't be negative." });

        validate(b.arrival, ({ valueOf }) => this.timeProblem(timeKey(valueOf(b.key), "arrival")));
        validate(b.handlingStart, ({ valueOf }) =>
          this.timeProblem(timeKey(valueOf(b.key), "handlingStart")),
        );
        validate(b.handlingFinish, ({ valueOf }) =>
          this.timeProblem(timeKey(valueOf(b.key), "handlingFinish")),
        );
        validate(b.departure, ({ valueOf }) => {
          const problem = this.timeProblem(timeKey(valueOf(b.key), "departure"));
          if (problem || valueOf(b.departureRecorded)) return problem;
          const hasArrival = valueOf(b.arrivalRecorded) || !!valueOf(b.arrival).trim();
          const typed = !!valueOf(b.departure).trim();
          if (typed && !hasArrival) {
            return { kind: "needsArrival", message: "Enter the arrival too." };
          }
          const handling =
            (!valueOf(b.handlingStartRecorded) && !!valueOf(b.handlingStart).trim()) ||
            (!valueOf(b.handlingFinishRecorded) && !!valueOf(b.handlingFinish).trim());
          return !typed && handling
            ? {
                kind: "needsDeparture",
                message: "Loading times are saved with the departure - enter it too.",
              }
            : null;
        });
        required(b.outcome, {
          message: "Pick the drop's outcome.",
          when: ({ valueOf }) =>
            valueOf(b.kind) === "drop" &&
            !valueOf(b.departureRecorded) &&
            !!valueOf(b.departure).trim(),
        });
        validate(b.returnReason, ({ value, valueOf }) =>
          !valueOf(b.departureRecorded) &&
          ((valueOf(b.returnedBoxes) ?? 0) > 0 || (valueOf(b.returnedBags) ?? 0) > 0) &&
          !value()
            ? { kind: "reasonRequired", message: "Say why they came back." }
            : null,
        );
        validate(b.note, ({ value, valueOf }) =>
          !valueOf(b.departureRecorded) &&
          (valueOf(b.outcome) === "foul_trip" || valueOf(b.outcome) === "cancelled_by_client") &&
          !value().trim()
            ? { kind: "noteRequired", message: "Say what happened." }
            : null,
        );
      });
    },
    {
      submission: {
        action: async () => {
          const command = buildCommand(this.model(), this.resolved());
          const nothingNew =
            command.stops!.length === 0 && !command.garageDepartedAt && !command.garageArrivedAt;
          if (nothingNew) {
            this.toast.showInfo("Nothing new to save.");
            return undefined;
          }

          try {
            await this.api.invoke(recordTripReport, { tripId: this.tripId, body: command });
            this.toast.showSuccess("Report saved");
            await this.load();
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Couldn't save the report."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    void this.load();
    void inject(TripPhotosStore).load(this.tripId);
  }

  /** Takes an unvisited drop off the trip; its load is deleted, as on Edit Trip. */
  protected removeDrop(info: BlockInfo): void {
    if (this.blocksUnsaved()) return;
    const name = info.load?.customer?.name;
    this.toast.confirm({
      header: `Remove ${info.title.toLowerCase()}`,
      message: `Take ${info.title.toLowerCase()}${name ? ` (${name})` : ""} off this trip? Its load is deleted.`,
      icon: "warning",
      severity: "danger",
      acceptLabel: "Remove",
      accept: () => void this.detachDrop(info),
    });
  }

  private async detachDrop(info: BlockInfo): Promise<void> {
    const trip = this.trip()!;
    try {
      await this.api.invoke(updateTrip, {
        id: trip.id!,
        body: headerUpdate(trip, { detachedLoadIds: [info.load!.id!] }),
      });
      this.toast.showSuccess(`${info.title} removed`);
      await this.load();
    } catch {
      // The API error interceptor has already shown why.
    }
  }

  /** Changing the trip reloads the page, which would drop times typed but not saved. */
  private blocksUnsaved(): boolean {
    if (!this.form().dirty()) return false;
    this.toast.showInfo("Save the report first - changing the trip reloads the page.");
    return true;
  }

  protected openStopCorrection(info: BlockInfo): void {
    this.correcting.set(info);
    this.stopCorrectionOpen.set(true);
  }

  /** Enter moves to the next field, the way the office reads down the message; Ctrl+S saves. */
  protected onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") {
      event.preventDefault();
      target.closest("form")?.requestSubmit();
      return;
    }

    if (event.key !== "Enter" || event.shiftKey || !(target instanceof HTMLInputElement)) {
      return;
    }

    const formEl = target.closest("form");
    const fields = Array.from(
      formEl?.querySelectorAll<HTMLInputElement>("input:not([readonly]):not([disabled])") ?? [],
    ).filter((el) => el.type !== "hidden" && el.offsetParent !== null);
    const next = fields[fields.indexOf(target) + 1];
    if (next) {
      event.preventDefault();
      next.focus();
      next.select();
    }
  }

  private timeProblem(key: string) {
    const at = this.resolved().get(key);
    if (at === "invalid") {
      return { kind: "badTime", message: "Type a time like 2329 or 23:29." };
    }
    return at instanceof Date && at.getTime() > Date.now() + FUTURE_SKEW_MS
      ? { kind: "future", message: "That's in the future - check the pre-load day." }
      : null;
  }

  protected async load(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [trip, corrections, incidents] = await Promise.all([
        this.api.invoke(getTripById, { tripId: this.tripId }),
        this.api.invoke(getTripCorrections, { tripId: this.tripId }),
        this.api.invoke(getIncidents, { TripId: this.tripId, PageSize: 50, OrderBy: "OccurredAt" }),
      ]);
      const blocks = buildBlocks(trip);
      this.trip.set(trip);
      this.corrections.set(corrections);
      this.incidents.set(incidents.items ?? []);
      this.blocks.set(blocks);
      this.form().reset(buildModel(trip, blocks));
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the trip."));
    } finally {
      this.isLoading.set(false);
    }
  }
}

/** A garage time the crew recorded from the Driver App says so next to its date. */
function withSource(hint: string | undefined, source: TripEventSource | undefined): string {
  return [hint, source === "driver_app" ? "from the app" : ""].filter(Boolean).join(" · ");
}
