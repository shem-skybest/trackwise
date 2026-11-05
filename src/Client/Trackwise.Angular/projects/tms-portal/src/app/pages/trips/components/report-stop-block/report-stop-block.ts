import { DatePipe } from "@angular/common";
import { Component, computed, input, output } from "@angular/core";
import { FormField, type FieldTree } from "@angular/forms/signals";
import { dropOutcomeOptions, dropReturnReasonOptions } from "@trackwise/shared/api/enums";
import {
  Badge,
  Grid,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
} from "@trackwise/shared/ui";
import {
  timeKey,
  type BlockInfo,
  type ReportBlock,
  type TimeField,
} from "../../trip-report/trip-report.model";
import { describeChange, type CorrectionGroup } from "../../utils/corrections";
import { ReportStopPhotos } from "../report-stop-photos/report-stop-photos";

/**
 * One block of the crew report - a warehouse visit or a drop - laid out in the message's order:
 * arrival, loading/unloading, departure, then for a drop what was delivered. Recorded values show
 * read-only; the rest take a typed time.
 */
@Component({
  selector: "app-report-stop-block",
  templateUrl: "./report-stop-block.html",
  imports: [
    Badge,
    DatePipe,
    FormField,
    Grid,
    ReportStopPhotos,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
  ],
})
export class ReportStopBlock {
  public readonly info = input.required<BlockInfo>();
  public readonly block = input.required<FieldTree<ReportBlock>>();
  /** The date each typed time resolved to, by time key. */
  public readonly hints = input<ReadonlyMap<string, string>>(new Map());
  /** Changes made to this block's recorded values, one group per save, newest first. */
  public readonly corrections = input<readonly CorrectionGroup[]>([]);
  /** Nothing is recorded on this drop or its pickup, so it can still be taken off the trip. */
  public readonly removable = input(false);
  /** The rescue truck that delivered this drop, when one took it over. */
  public readonly rescuedBy = input<string | null>(null);
  public readonly correct = output<void>();
  public readonly remove = output<void>();

  protected readonly describeChange = describeChange;
  /** Only what's recorded can be corrected; the rest is still typed into the report. */
  protected readonly canCorrect = computed(() => !!this.info().stop.arrivedAt);

  protected readonly outcomeOptions = dropOutcomeOptions;
  protected readonly reasonOptions = dropReturnReasonOptions;

  protected readonly isDrop = computed(() => this.info().kind === "drop");
  /** Any of the stop's times came from the crew's Driver App rather than the office. */
  protected readonly fromApp = computed(() => {
    const { stop } = this.info();
    return [
      stop.arrivalSource,
      stop.handlingStartedSource,
      stop.handlingFinishedSource,
      stop.departureSource,
    ].includes("driver_app");
  });
  /** Element ids start with a letter: a bare GUID can begin with a digit, which CSS can't select. */
  protected readonly idPrefix = computed(() => `stop-${this.info().key}`);
  protected readonly handlingLabel = computed(() => (this.isDrop() ? "unloading" : "loading"));

  protected readonly planned = computed(() => {
    const load = this.info().load;
    if (!load) return "";
    const parts = [
      load.plannedBoxes ? `${load.plannedBoxes} boxes` : "",
      load.plannedBags ? `${load.plannedBags} bags` : "",
    ].filter(Boolean);
    return parts.join(" + ");
  });

  protected readonly where = computed(() => {
    const { stop } = this.info();
    const address = stop.address;
    return [address?.streetAddress, address?.cityOrMunicipality].filter(Boolean).join(", ");
  });

  protected readonly stopIds = computed(() => this.info().stops.map((s) => s.id!));

  /** For a warehouse visit: how many loads it picks up. */
  protected readonly visitSize = computed(() =>
    this.info().kind === "pickup" ? this.info().stops.length : 0,
  );

  protected hint(field: TimeField): string {
    return this.hints().get(timeKey(this.info().key, field)) ?? "";
  }

  /** What the recorded times work out to - calculated by the API, never typed. */
  protected readonly stats = computed(() => {
    const { stop } = this.info();
    if (stop.dwellMinutes == null) return [];
    const handling = this.isDrop() ? "Unloading" : "Loading";
    return [
      stop.waitingMinutes == null ? "" : `Waiting ${formatMinutes(stop.waitingMinutes)}`,
      stop.handlingMinutes == null ? "" : `${handling} ${formatMinutes(stop.handlingMinutes)}`,
      `Dwell ${formatMinutes(stop.dwellMinutes)}`,
      stop.departureRecordedBy ? `Entered by ${stop.departureRecordedBy}` : "",
    ].filter(Boolean);
  });
}

function formatMinutes(value: number): string {
  const hours = Math.floor(value / 60);
  const mins = value % 60;
  return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
}
