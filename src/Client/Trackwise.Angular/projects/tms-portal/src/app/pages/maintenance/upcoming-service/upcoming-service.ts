import { DatePipe, DecimalPipe } from "@angular/common";
import { Component, inject, signal, type OnInit } from "@angular/core";
import { Router } from "@angular/router";
import type { MaintenanceScheduleDto } from "@trackwise/shared/api";
import { LocalizationService } from "@trackwise/shared/services";
import {
  Badge,
  Card,
  Icon,
  UiButton,
  UiDataTable,
  UiMenu,
  UiSortHeader,
  type UiBadgeIntent,
  type UiMenuItem,
} from "@trackwise/shared/ui";
import { DataContainer, PageHeader } from "@/shared/components";
import { UpcomingServiceStore } from "../store";

@Component({
  selector: "app-upcoming-service",
  templateUrl: "./upcoming-service.html",
  providers: [UpcomingServiceStore],
  imports: [
    Badge,
    Card,
    DataContainer,
    DatePipe,
    DecimalPipe,
    Icon,
    PageHeader,
    UiButton,
    UiDataTable,
    UiMenu,
    UiSortHeader,
  ],
})
export class UpcomingServicePage implements OnInit {
  private readonly router = inject(Router);
  protected readonly store = inject(UpcomingServiceStore);
  protected readonly distanceUnit = inject(LocalizationService).getDistanceUnitLabel();

  protected readonly selectedRow = signal<MaintenanceScheduleDto | null>(null);

  protected readonly actionMenuItems: UiMenuItem[] = [
    {
      label: "Log service",
      icon: "check",
      command: () => this.logService(this.selectedRow()!),
    },
    {
      label: "View truck",
      icon: "truck",
      command: () => this.viewTruck(this.selectedRow()!),
    },
  ];

  ngOnInit(): void {
    this.store.load();
  }

  protected getStatusSeverity(schedule: MaintenanceScheduleDto): UiBadgeIntent {
    if (schedule.isOverdue) {
      return "danger";
    }
    if ((schedule.daysUntilDue ?? 0) <= 7) {
      return "warn";
    }
    return "info";
  }

  protected getStatusLabel(schedule: MaintenanceScheduleDto): string {
    if (schedule.isOverdue) {
      return "Overdue";
    }
    if ((schedule.daysUntilDue ?? 0) <= 7) {
      return "Due Soon";
    }
    return "Scheduled";
  }

  protected intervalLabel(schedule: MaintenanceScheduleDto): string {
    const parts = [
      schedule.daysInterval ? `${schedule.daysInterval} days` : null,
      schedule.mileageInterval
        ? `${schedule.mileageInterval.toLocaleString()} ${this.distanceUnit}`
        : null,
      schedule.engineHoursInterval ? `${schedule.engineHoursInterval} engine hrs` : null,
    ].filter(Boolean);
    return parts.length ? `Every ${parts.join(" or ")}` : "-";
  }

  protected logService(schedule: MaintenanceScheduleDto): void {
    this.router.navigate(["/maintenance/records/add"], {
      queryParams: {
        truckId: schedule.truckId,
        truckNumber: schedule.truckNumber,
        type: schedule.type,
        scheduleId: schedule.id,
      },
    });
  }

  protected viewTruck(schedule: MaintenanceScheduleDto): void {
    this.router.navigateByUrl(`/master-data/trucks/${schedule.truckId}`);
  }
}
