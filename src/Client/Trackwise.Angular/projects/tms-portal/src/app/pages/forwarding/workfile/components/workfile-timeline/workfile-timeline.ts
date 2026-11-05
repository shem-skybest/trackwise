import { Component, input } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { WorkfileTimelineEntryDto, WorkfileTimelineEventKind } from "@trackwise/shared/api";
import { DateFormatPipe } from "@trackwise/shared/pipes";
import {
  Card,
  Icon,
  Stack,
  Typography,
  UiTimeline,
  UiTimelineContent,
  UiTimelineMarker,
  type IconName,
} from "@trackwise/shared/ui";

interface KindView {
  label: string;
  icon: IconName;
  marker: string;
}

const KINDS: Record<WorkfileTimelineEventKind, KindView> = {
  pickup_planned: {
    label: "Truck pickup planned at",
    icon: "calendar",
    marker: "bg-muted text-muted-foreground",
  },
  truck_arrived_at_pickup: {
    label: "Truck arrived at",
    icon: "truck",
    marker: "bg-warning/15 text-warning",
  },
  truck_left_pickup: { label: "Truck left", icon: "navigation", marker: "bg-info/15 text-info" },
  truck_arrived_at_drop: {
    label: "Truck arrived at",
    icon: "truck",
    marker: "bg-warning/15 text-warning",
  },
  truck_left_drop: { label: "Dropped at", icon: "check", marker: "bg-success/15 text-success" },
  vessel_departed: {
    label: "Vessel departs",
    icon: "ship",
    marker: "bg-primary text-primary-foreground",
  },
  vessel_arrived: {
    label: "Vessel arrives",
    icon: "ship",
    marker: "bg-primary text-primary-foreground",
  },
};

/**
 * A forwarding workfile's door-to-door steps in move order: legs that bring the boxes to the port,
 * the vessel, then legs that take them away. Vessel dates are the ETD/ETA staff typed; truck steps
 * are what the crew or the office recorded on the leg's trip.
 */
@Component({
  selector: "app-workfile-timeline",
  templateUrl: "./workfile-timeline.html",
  imports: [
    Card,
    DateFormatPipe,
    Icon,
    RouterLink,
    Stack,
    Typography,
    UiTimeline,
    UiTimelineContent,
    UiTimelineMarker,
  ],
})
export class WorkfileTimeline {
  public readonly entries = input.required<WorkfileTimelineEntryDto[]>();
  public readonly vesselName = input<string | null | undefined>(null);

  protected kind(entry: WorkfileTimelineEntryDto): KindView {
    return (
      KINDS[entry.kind!] ?? {
        label: entry.kind ?? "-",
        icon: "circle",
        marker: "bg-muted text-muted-foreground",
      }
    );
  }

  protected isVessel(entry: WorkfileTimelineEntryDto): boolean {
    return entry.kind === "vessel_departed" || entry.kind === "vessel_arrived";
  }
}
