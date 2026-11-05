import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Permission } from "@trackwise/shared";
import type { IncidentDto, IncidentKind, IncidentStatus } from "@trackwise/shared/api";
import { incidentKindOptions, incidentStatusOptions } from "@trackwise/shared/api/enums";
import {
  Badge,
  Card,
  Grid,
  SearchField,
  Stack,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSelectField,
  UiSortHeader,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { DataContainer, PageHeader } from "@/shared/components";
import { IncidentsListStore } from "../store/incidents-list.store";
import {
  incidentKindLabel,
  incidentSeverityIntent,
  incidentSeverityLabel,
  incidentStatusIntent,
  incidentStatusLabel,
} from "../utils/incident-labels";

/**
 * Every incident, newest first - the cases Cold Chain numbered in Monday's weekly report: accidents,
 * breakdowns, damaged or missing goods, loading and paperwork mistakes.
 */
@Component({
  selector: "app-incidents-list",
  templateUrl: "./incidents-list.html",
  providers: [IncidentsListStore],
  imports: [
    Badge,
    Card,
    DataContainer,
    DatePipe,
    Grid,
    PageHeader,
    RouterLink,
    Stack,
    UiButton,
    UiDataTable,
    UiFormField,
    SearchField,
    UiSelectField,
    UiSortHeader,
  ],
})
export class IncidentsList {
  private readonly router = inject(Router);

  protected readonly store = inject(IncidentsListStore);
  protected readonly canManage = inject(PermissionService).hasPermission(Permission.Safety.Manage);
  protected readonly kind = signal<IncidentKind | null>(null);
  protected readonly status = signal<IncidentStatus | null>(null);

  protected readonly kindOptions = incidentKindOptions;
  protected readonly statusOptions = incidentStatusOptions;
  protected readonly kindLabel = incidentKindLabel;
  protected readonly statusLabel = incidentStatusLabel;
  protected readonly statusIntent = incidentStatusIntent;
  protected readonly severityLabel = incidentSeverityLabel;
  protected readonly severityIntent = incidentSeverityIntent;

  protected onKindChange(kind: IncidentKind | null): void {
    this.kind.set(kind);
    this.applyFilters();
  }

  protected onStatusChange(status: IncidentStatus | null): void {
    this.status.set(status);
    this.applyFilters();
  }

  protected crew(incident: IncidentDto): string {
    return [incident.driverName, incident.helperName, incident.secondHelperName]
      .filter(Boolean)
      .join(" · ");
  }

  protected open(incident: IncidentDto): void {
    void this.router.navigate(["/incidents", incident.id]);
  }

  private applyFilters(): void {
    this.store.setFilters({
      Kind: this.kind() ?? undefined,
      Status: this.status() ?? undefined,
    });
  }
}
