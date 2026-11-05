import { Component, computed, inject, signal } from "@angular/core";
import type { TripLoadDto, TripStopDto } from "@trackwise/shared/api";
import { dropReturnReasonOptions, loadStatusOptions } from "@trackwise/shared/api/enums";
import { AddressPipe, DateFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Icon,
  Stack,
  Typography,
  UiButton,
  UiDataTable,
  UiSortHeader,
  UiTableRowDirectives,
} from "@trackwise/shared/ui";
import { TripDetailsStore } from "../../store/trip-details.store";
import { sameVisitPickups } from "../../utils/same-visit";
import { GarageTimesDialog } from "../garage-times-dialog/garage-times-dialog";
import { StopEventDialog, type StopEventMode } from "../stop-event-dialog/stop-event-dialog";

/**
 * The trip's stops as the office logs them: reported arrival and departure, loading/unloading,
 * temperatures, cargo in and out, the drop's outcome and whether it beat its cut-off. On-time,
 * waiting, handling and dwell are calculated by the API, never typed.
 */
@Component({
  selector: "app-trip-stops-card",
  templateUrl: "./trip-stops-card.html",
  imports: [
    AddressPipe,
    Badge,
    Card,
    DateFormatPipe,
    GarageTimesDialog,
    Icon,
    Stack,
    StopEventDialog,
    Typography,
    UiButton,
    UiDataTable,
    UiSortHeader,
    UiTableRowDirectives,
  ],
})
export class TripStopsCard {
  protected readonly store = inject(TripDetailsStore);

  protected readonly dialogOpen = signal(false);
  protected readonly dialogMode = signal<StopEventMode>("arrive");
  protected readonly dialogStop = signal<TripStopDto | null>(null);
  protected readonly garageDialogOpen = signal(false);

  private readonly loadsById = computed(
    () => new Map((this.store.trip()?.loads ?? []).map((l) => [l.id, l] as const)),
  );

  protected readonly dialogLoad = computed(() => this.loadFor(this.dialogStop()));
  protected readonly dialogVisitSize = computed(() => {
    const stop = this.dialogStop();
    return stop ? this.sameVisitPickups(stop).length : 1;
  });

  protected readonly canRecordGarage = computed(() => {
    const status = this.store.trip()?.status;
    return status === "dispatched" || status === "in_transit" || status === "completed";
  });

  protected loadFor(stop: TripStopDto | null): TripLoadDto | null {
    return stop?.loadId ? (this.loadsById().get(stop.loadId) ?? null) : null;
  }

  protected stopLabel(stop: TripStopDto): string {
    return stop.type === "pick_up" ? "Pickup" : "Drop";
  }

  protected openEvent(stop: TripStopDto, mode: StopEventMode): void {
    this.dialogStop.set(stop);
    this.dialogMode.set(mode);
    this.dialogOpen.set(true);
  }

  protected outcomeLabel(load: TripLoadDto | null): string | null {
    if (!load) return null;
    return loadStatusOptions.find((o) => o.value === load.status)?.label ?? null;
  }

  protected reasonLabel(load: TripLoadDto | null): string | null {
    const reason = load?.returnReason;
    return reason ? (dropReturnReasonOptions.find((o) => o.value === reason)?.label ?? null) : null;
  }

  protected minutes(value: number | null | undefined): string {
    if (value == null) return "";
    const hours = Math.floor(value / 60);
    const mins = value % 60;
    return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  }

  private sameVisitPickups(stop: TripStopDto): TripStopDto[] {
    return sameVisitPickups(stop, this.store.sortedStops());
  }
}
