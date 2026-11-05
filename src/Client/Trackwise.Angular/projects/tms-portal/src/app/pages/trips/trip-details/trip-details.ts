import { CommonModule } from "@angular/common";
import { Component, computed, effect, inject, input } from "@angular/core";
import { RouterLink } from "@angular/router";
import { ToastService } from "@trackwise/shared";
import {
  AddressPipe,
  CurrencyFormatPipe,
  DateFormatPipe,
  DistanceUnitPipe,
} from "@trackwise/shared/pipes";
import { LocalizationService } from "@trackwise/shared/services";
import {
  Card,
  EmptyState,
  Grid,
  Icon,
  Progress,
  Skeleton,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDataTable,
  UiSortHeader,
  UiTableRowDirectives,
} from "@trackwise/shared/ui";
import {
  DirectionMap,
  LoadStatusTag,
  PageHeader,
  TripStatusTag,
  type RouteSegmentClickEvent,
  type Waypoint,
  type WaypointClickEvent,
} from "@/shared/components";
import { TripActions, TripRateCard, TripStopsCard, TripTimeline } from "../components";
import { TripDetailsStore } from "../store/trip-details.store";

@Component({
  selector: "app-trip-details",
  templateUrl: "./trip-details.html",
  providers: [TripDetailsStore],
  imports: [
    AddressPipe,
    Card,
    CommonModule,
    CurrencyFormatPipe,
    DateFormatPipe,
    DirectionMap,
    DistanceUnitPipe,
    EmptyState,
    Grid,
    Icon,
    LoadStatusTag,
    PageHeader,
    Progress,
    RouterLink,
    Skeleton,
    Stack,
    Surface,
    TripActions,
    TripRateCard,
    TripStopsCard,
    TripStatusTag,
    TripTimeline,
    Typography,
    UiButton,
    UiDataTable,
    UiSortHeader,
    UiTableRowDirectives,
  ],
})
export class TripDetailsPage {
  protected readonly store = inject(TripDetailsStore);
  private readonly toastService = inject(ToastService);
  private readonly localizationService = inject(LocalizationService, { optional: true });

  protected readonly tripId = input<string>();

  protected readonly distanceUnitLabel = computed(
    () => this.localizationService?.getDistanceUnitLabel() ?? "mi",
  );

  // Transform TripStopDto[] to Waypoint[] for DirectionMap
  protected readonly waypoints = computed<Waypoint[]>(() =>
    this.store
      .sortedStops()
      .filter((stop) => stop.id != null)
      .map((stop) => ({
        id: stop.id!,
        location: stop.location,
      })),
  );

  // Transform selectedStop to Waypoint | null for DirectionMap
  protected readonly selectedWaypoint = computed<Waypoint | null>(() => {
    const stop = this.store.selectedStop();
    if (!stop || !stop.id) return null;
    return { id: stop.id, location: stop.location };
  });

  constructor() {
    // React to tripId changes
    effect(() => {
      const id = this.tripId();
      if (id) {
        this.store.loadTrip(id);
      }
    });
  }

  protected onRouteSegmentClick(e: RouteSegmentClickEvent): void {
    const stop = this.store.trip()?.stops?.find((s) => s.id === e.fromWaypoint.id);
    this.store.selectStop(stop ?? null);
  }

  protected onWaypointClick(e: WaypointClickEvent): void {
    const stop = this.store.trip()?.stops?.find((s) => s.id === e.waypoint.id);
    this.store.selectStop(stop ?? null);
  }

  // A refusal is already toasted with the server's reason by the global error handler.
  protected async onDispatch(): Promise<void> {
    const warnings = await this.store.dispatchTrip();
    if (!warnings) return;

    this.toastService.showSuccess("The trip has been dispatched successfully.", "Trip Dispatched");
    for (const warning of warnings) {
      this.toastService.showWarning(warning, "Check before departure");
    }
  }

  protected async onCancel(reason?: string): Promise<void> {
    if (await this.store.cancelTrip(reason)) {
      this.toastService.showInfo("The trip has been cancelled.", "Trip Cancelled");
    }
  }
}
