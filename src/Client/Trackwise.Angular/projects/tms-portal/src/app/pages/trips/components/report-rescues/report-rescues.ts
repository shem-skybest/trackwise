import { DatePipe } from "@angular/common";
import { Component, computed, inject, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  undoTripRescue,
  type IncidentDto,
  type TripDto,
  type TripRescueDto,
} from "@trackwise/shared/api";
import { Badge, Stack, Surface, Typography, UiButton } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import {
  incidentKindLabel,
  incidentStatusIntent,
  incidentStatusLabel,
} from "@/pages/incidents/utils/incident-labels";
import { canRescue, canUndoRescue, dropNumber } from "../../utils/rescue";
import { RescueDialog } from "../rescue-dialog/rescue-dialog";
import { RescueGarageDialog } from "../rescue-garage-dialog/rescue-garage-dialog";

/**
 * What went wrong on the trip, under the crew report's header: the incidents on record and any
 * rescue - another truck that took over drops. Rescuing and reporting an incident start here.
 */
@Component({
  selector: "app-report-rescues",
  templateUrl: "./report-rescues.html",
  imports: [
    Badge,
    DatePipe,
    RescueDialog,
    RescueGarageDialog,
    RouterLink,
    Stack,
    Surface,
    Typography,
    UiButton,
  ],
})
export class ReportRescues {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  public readonly incidents = input<readonly IncidentDto[]>([]);
  /** The report below has typed times not saved yet - a change here reloads the page. */
  public readonly unsaved = input(false);
  public readonly changed = output<void>();

  protected readonly dialogOpen = signal(false);
  protected readonly garageOpen = signal(false);
  protected readonly garageRescue = signal<TripRescueDto | null>(null);
  protected readonly rescuable = computed(() => canRescue(this.trip()));
  protected readonly rescues = computed(() => this.trip().rescues ?? []);
  protected readonly kindLabel = incidentKindLabel;
  protected readonly statusLabel = incidentStatusLabel;
  protected readonly statusIntent = incidentStatusIntent;

  protected drops(rescue: TripRescueDto): string {
    const numbers = (rescue.stopIds ?? []).map((id) => dropNumber(this.trip(), id));
    return `Drop${numbers.length === 1 ? "" : "s"} ${numbers.join(", ")}`;
  }

  protected crew(rescue: TripRescueDto): string {
    return [rescue.driverName, rescue.helperName, rescue.secondHelperName]
      .filter(Boolean)
      .join(" · ");
  }

  protected undoable(rescue: TripRescueDto): boolean {
    return canUndoRescue(this.trip(), rescue);
  }

  protected openRescue(): void {
    if (this.unsaved()) {
      this.toast.showInfo("Save the report first - recording a rescue reloads the page.");
      return;
    }
    this.dialogOpen.set(true);
  }

  protected openGarage(rescue: TripRescueDto): void {
    if (this.unsaved()) {
      this.toast.showInfo("Save the report first - saving garage times reloads the page.");
      return;
    }
    this.garageRescue.set(rescue);
    this.garageOpen.set(true);
  }

  protected undo(rescue: TripRescueDto): void {
    if (this.unsaved()) {
      this.toast.showInfo("Save the report first - undoing a rescue reloads the page.");
      return;
    }
    this.toast.confirm({
      header: "Undo rescue",
      message: `Give ${this.drops(rescue).toLowerCase()} back to ${this.trip().truckNumber ?? "the trip's truck"}?`,
      icon: "warning",
      severity: "danger",
      acceptLabel: "Undo rescue",
      accept: () => void this.undoRescue(rescue),
    });
  }

  private async undoRescue(rescue: TripRescueDto): Promise<void> {
    try {
      await this.api.invoke(undoTripRescue, { tripId: this.trip().id!, rescueId: rescue.id! });
      this.toast.showSuccess("Rescue undone");
      this.changed.emit();
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't undo the rescue."));
    }
  }
}
