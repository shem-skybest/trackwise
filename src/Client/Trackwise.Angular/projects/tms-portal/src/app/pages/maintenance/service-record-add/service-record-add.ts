import { Component, computed, inject, input } from "@angular/core";
import { Router } from "@angular/router";
import type { MaintenanceRecordDto, MaintenanceType, TruckDto } from "@trackwise/shared/api";
import { Card } from "@trackwise/shared/ui";
import { PageHeader } from "@/shared/components";
import {
  MaintenanceRecordForm,
  type MaintenanceRecordFormValue,
} from "../components/maintenance-record-form/maintenance-record-form";

/**
 * Logs a service. Opened from a due schedule (upcoming service, a truck's maintenance tab), the
 * query string pre-fills the truck, the type and the schedule it moves on.
 */
@Component({
  selector: "app-service-record-add",
  templateUrl: "./service-record-add.html",
  imports: [Card, MaintenanceRecordForm, PageHeader],
})
export class ServiceRecordAddPage {
  private readonly router = inject(Router);

  public readonly truckId = input<string>();
  public readonly truckNumber = input<string>();
  public readonly type = input<MaintenanceType>();
  public readonly scheduleId = input<string>();

  protected readonly initial = computed<Partial<MaintenanceRecordFormValue> | null>(() => {
    const truckId = this.truckId();
    if (!truckId) {
      return null;
    }

    return {
      truck: { id: truckId, number: this.truckNumber() ?? "" } as TruckDto,
      maintenanceScheduleId: this.scheduleId() ?? null,
      ...(this.type() ? { type: this.type()! } : {}),
      ...(this.type() === "reefer_pms"
        ? { vehicleSystem: "reefer" as const, description: "Reefer PMS" }
        : {}),
    };
  });

  protected onSave(record: MaintenanceRecordDto): void {
    this.router.navigateByUrl(`/maintenance/records/${record.id}`);
  }
}
