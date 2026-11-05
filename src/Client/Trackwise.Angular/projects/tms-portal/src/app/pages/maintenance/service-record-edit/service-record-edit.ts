import { Component, inject, input, signal, type OnInit } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  getMaintenanceRecordById,
  type MaintenanceRecordDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { Card, Spinner, Stack } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import {
  MaintenanceRecordForm,
  type MaintenanceRecordFormValue,
} from "../components/maintenance-record-form/maintenance-record-form";

@Component({
  selector: "app-service-record-edit",
  templateUrl: "./service-record-edit.html",
  imports: [Card, MaintenanceRecordForm, PageHeader, Spinner, Stack],
})
export class ServiceRecordEditPage implements OnInit {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);

  public readonly id = input.required<string>();

  protected readonly isLoading = signal(true);
  protected readonly record = signal<MaintenanceRecordDto | null>(null);
  protected readonly initialFormValue = signal<Partial<MaintenanceRecordFormValue> | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const found = await this.api.invoke(getMaintenanceRecordById, { id: this.id() });
      if (!found) {
        this.toastService.showError("Maintenance record not found");
        this.router.navigateByUrl("/maintenance/records");
        return;
      }

      this.record.set(found);
      this.initialFormValue.set({
        truck: { id: found.truckId, number: found.truckNumber } as TruckDto,
        maintenanceScheduleId: found.maintenanceScheduleId ?? null,
        type: found.type,
        vehicleSystem: found.vehicleSystem ?? null,
        description: found.description ?? "",
        serviceDate: found.serviceDate ? new Date(found.serviceDate) : new Date(),
        odometerReading: found.odometerReading ?? null,
        engineHours: found.engineHours ?? null,
        requisitionNumber: found.requisitionNumber ?? "",
        vendorName: found.vendorName ?? "",
        invoiceNumber: found.invoiceNumber ?? "",
        laborCost: found.laborCost ?? 0,
        partsCost: found.partsCost ?? 0,
        notes: found.notes ?? "",
        parts: (found.parts ?? []).map((p) => ({
          partName: p.partName ?? "",
          description: p.description ?? "",
          quantity: p.quantity ?? 1,
          unitCost: p.unitCost ?? 0,
        })),
      });
    } finally {
      this.isLoading.set(false);
    }
  }

  protected onSave(record: MaintenanceRecordDto): void {
    this.router.navigateByUrl(`/maintenance/records/${record.id}`);
  }
}
