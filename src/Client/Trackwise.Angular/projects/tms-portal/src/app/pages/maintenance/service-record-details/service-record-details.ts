import { DatePipe, DecimalPipe } from "@angular/common";
import { Component, computed, inject, input, signal, type OnInit } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import {
  Api,
  deleteMaintenanceRecord,
  getMaintenanceRecordById,
  type MaintenanceRecordDto,
} from "@trackwise/shared/api";
import { vehicleSystemOptions } from "@trackwise/shared/api/enums";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { LocalizationService, ToastService } from "@trackwise/shared/services";
import {
  Badge,
  Card,
  Grid,
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
} from "@trackwise/shared/ui";
import { PageHeader } from "@/shared/components";

@Component({
  selector: "app-service-record-details",
  templateUrl: "./service-record-details.html",
  imports: [
    Badge,
    Card,
    CurrencyFormatPipe,
    DatePipe,
    DecimalPipe,
    Grid,
    Icon,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    Typography,
    UiButton,
  ],
})
export class ServiceRecordDetailsPage implements OnInit {
  private readonly router = inject(Router);
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  protected readonly distanceUnit = inject(LocalizationService).getDistanceUnitLabel();

  public readonly id = input.required<string>();

  protected readonly isLoading = signal(true);
  protected readonly record = signal<MaintenanceRecordDto | null>(null);

  protected readonly pageTitle = computed(() => {
    const rec = this.record();
    return rec
      ? `${rec.typeDisplay ?? "Service"} - ${rec.truckNumber ?? "Unknown Truck"}`
      : "Service Record";
  });

  protected readonly systemLabel = computed(
    () => vehicleSystemOptions.find((o) => o.value === this.record()?.vehicleSystem)?.label ?? null,
  );

  async ngOnInit(): Promise<void> {
    try {
      const found = await this.api.invoke(getMaintenanceRecordById, { id: this.id() });
      if (found) {
        this.record.set(found);
      } else {
        this.toast.showError("Maintenance record not found");
        this.router.navigateByUrl("/maintenance/records");
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  protected editRecord(): void {
    this.router.navigateByUrl(`/maintenance/records/${this.id()}/edit`);
  }

  protected deleteRecord(): void {
    this.toast.confirm({
      header: "Delete service record",
      message: "Delete this service record? Only do this for one logged by mistake.",
      acceptLabel: "Delete",
      rejectLabel: "Cancel",
      severity: "danger",
      accept: async () => {
        await this.api.invoke(deleteMaintenanceRecord, { id: this.id() });
        this.toast.showSuccess("Service record deleted");
        this.router.navigateByUrl("/maintenance/records");
      },
    });
  }
}
