import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { getApiErrorMessage, Permission } from "@trackwise/shared";
import {
  Api,
  closeIncident,
  deleteIncident,
  getIncidentById,
  reopenIncident,
  resolveIncident,
  reviewIncident,
  type IncidentDto,
  type TripRescueDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Grid,
  InfoItem,
  Spinner,
  Stack,
  Typography,
  UiButton,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { IncidentAttachments } from "../components/incident-attachments/incident-attachments";
import { IncidentNoteDialog } from "../components/incident-note-dialog/incident-note-dialog";
import { IncidentRepairsDialog } from "../components/incident-repairs-dialog/incident-repairs-dialog";
import {
  accidentTypeLabel,
  incidentKindLabel,
  incidentSeverityIntent,
  incidentSeverityLabel,
  incidentStatusIntent,
  incidentStatusLabel,
} from "../utils/incident-labels";

type NoteStep = "review" | "resolve" | "reopen";

const NOTE_STEPS: Record<
  NoteStep,
  { header: string; label: string; hint: string; confirm: string; required: boolean }
> = {
  review: {
    header: "Put under review",
    label: "Notes",
    hint: "Optional - e.g. waiting on the police report, checking who pays",
    confirm: "Start review",
    required: false,
  },
  resolve: {
    header: "Resolve incident",
    label: "Corrective action",
    hint: "What was done about it, and what stops it happening again",
    confirm: "Resolve",
    required: true,
  },
  reopen: {
    header: "Reopen incident",
    label: "Why",
    hint: "e.g. the client came back about it",
    confirm: "Reopen",
    required: true,
  },
};

/**
 * One incident: what happened, who and what was involved, the reports and the money - linked
 * repairs plus what was paid to others - the rescue it called for, and its review.
 */
@Component({
  selector: "app-incident-details",
  templateUrl: "./incident-details.html",
  imports: [
    Badge,
    Card,
    CurrencyFormatPipe,
    DatePipe,
    Grid,
    IncidentAttachments,
    IncidentNoteDialog,
    IncidentRepairsDialog,
    InfoItem,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    Typography,
    UiButton,
  ],
})
export class IncidentDetails {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  protected readonly id = inject(ActivatedRoute).snapshot.paramMap.get("id")!;
  protected readonly canManage = inject(PermissionService).hasPermission(Permission.Safety.Manage);

  protected readonly incident = signal<IncidentDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isBusy = signal(false);
  protected readonly noteStep = signal<NoteStep>("resolve");
  protected readonly noteDialogOpen = signal(false);
  protected readonly repairsDialogOpen = signal(false);

  protected readonly note = computed(() => NOTE_STEPS[this.noteStep()]);
  protected readonly status = computed(() => this.incident()?.status);
  protected readonly editable = computed(
    () => this.status() === "open" || this.status() === "under_review",
  );
  protected readonly deletable = computed(
    () => this.status() === "open" && !this.incident()?.reviewedAt,
  );
  protected readonly isAccident = computed(() => this.incident()?.kind === "accident");
  protected readonly repairCost = computed(() =>
    (this.incident()?.repairs ?? []).reduce((sum, r) => sum + (r.totalCost ?? 0), 0),
  );

  protected readonly kindLabel = incidentKindLabel;
  protected readonly statusLabel = incidentStatusLabel;
  protected readonly statusIntent = incidentStatusIntent;
  protected readonly severityLabel = incidentSeverityLabel;
  protected readonly severityIntent = incidentSeverityIntent;
  protected readonly accidentTypeLabel = accidentTypeLabel;

  constructor() {
    void this.load();
  }

  protected crew(): string {
    const i = this.incident();
    return [i?.driverName, i?.helperName, i?.secondHelperName].filter(Boolean).join(" · ") || "-";
  }

  protected rescueCrew(rescue: TripRescueDto): string {
    return [rescue.driverName, rescue.helperName, rescue.secondHelperName]
      .filter(Boolean)
      .join(" · ");
  }

  protected ask(step: NoteStep): void {
    this.noteStep.set(step);
    this.noteDialogOpen.set(true);
  }

  protected async onNote(text: string): Promise<void> {
    const id = this.id;
    const actions: Record<NoteStep, () => Promise<IncidentDto>> = {
      review: () => this.api.invoke(reviewIncident, { id, body: { notes: text || null } }),
      resolve: () => this.api.invoke(resolveIncident, { id, body: { correctiveAction: text } }),
      reopen: () => this.api.invoke(reopenIncident, { id, body: { reason: text } }),
    };
    const done: Record<NoteStep, string> = {
      review: "Under review",
      resolve: "Incident resolved",
      reopen: "Incident reopened",
    };
    if (await this.run(actions[this.noteStep()], done[this.noteStep()])) {
      this.noteDialogOpen.set(false);
    }
  }

  protected close(): void {
    this.toast.confirm({
      header: "Close incident",
      message: "Close it for good? A closed incident can't be changed or reopened.",
      acceptLabel: "Close",
      accept: () =>
        void this.run(() => this.api.invoke(closeIncident, { id: this.id }), "Incident closed"),
    });
  }

  protected remove(): void {
    this.toast.confirm({
      header: "Delete incident",
      message: "Delete this report? Use this only for one made by mistake.",
      icon: "warning",
      severity: "danger",
      acceptLabel: "Delete",
      accept: () => void this.deleteIncident(),
    });
  }

  /** A child saved something and handed back the incident as it now stands. */
  protected onSaved(incident: IncidentDto): void {
    this.incident.set(incident);
  }

  private async deleteIncident(): Promise<void> {
    try {
      await this.api.invoke(deleteIncident, { id: this.id });
      this.toast.showSuccess("Incident deleted");
      void this.router.navigate(["/incidents"]);
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't delete the incident."));
    }
  }

  private async run(action: () => Promise<IncidentDto>, done: string): Promise<boolean> {
    this.isBusy.set(true);
    try {
      this.incident.set(await action());
      this.toast.showSuccess(done);
      return true;
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "That didn't work."));
      return false;
    } finally {
      this.isBusy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      this.incident.set(await this.api.invoke(getIncidentById, { id: this.id }));
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Couldn't load the incident."));
    } finally {
      this.isLoading.set(false);
    }
  }
}
