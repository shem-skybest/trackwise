import { Component, computed, effect, inject, input, output, signal } from "@angular/core";
import {
  applyEach,
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import {
  Api,
  createMaintenanceRecord,
  updateMaintenanceRecord,
  type CreateMaintenanceRecordCommand,
  type MaintenanceRecordDto,
  type MaintenanceType,
  type TruckDto,
  type VehicleSystem,
} from "@trackwise/shared/api";
import { maintenanceTypeOptions, vehicleSystemOptions } from "@trackwise/shared/api/enums";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { LocalizationService } from "@trackwise/shared/services";
import {
  CurrencyField,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDateField,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchTruck } from "@/shared/components/search";

export interface MaintenancePartRow {
  partName: string;
  description: string;
  quantity: number | null;
  unitCost: number | null;
}

export interface MaintenanceRecordFormValue {
  truck: TruckDto | null;
  maintenanceScheduleId: string | null;
  type: MaintenanceType;
  vehicleSystem: VehicleSystem | null;
  description: string;
  serviceDate: Date;
  odometerReading: number | null;
  engineHours: number | null;
  requisitionNumber: string;
  vendorName: string;
  invoiceNumber: string;
  laborCost: number | null;
  partsCost: number | null;
  notes: string;
  parts: MaintenancePartRow[];
}

const emptyPart = (): MaintenancePartRow => ({
  partName: "",
  description: "",
  quantity: 1,
  unitCost: null,
});

/**
 * A service or repair on one truck, as Motorpool's requisition lists it: the shop, the request for
 * payment it was paid through, and a line per part. Parts cost is the lines' sum; with no lines it
 * can be typed from the invoice total.
 */
@Component({
  selector: "app-maintenance-record-form",
  templateUrl: "./maintenance-record-form.html",
  imports: [
    CurrencyField,
    CurrencyFormatPipe,
    FormField,
    FormRoot,
    RouterLink,
    SearchTruck,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiDateField,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class MaintenanceRecordForm {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly localization = inject(LocalizationService);

  protected readonly typeOptions = maintenanceTypeOptions;
  protected readonly systemOptions = [
    { label: "Not specified", value: null },
    ...vehicleSystemOptions,
  ];
  protected readonly currency = this.localization.getCurrencySymbol();
  protected readonly distanceUnit = this.localization.getDistanceUnitLabel();

  public readonly mode = input.required<"create" | "edit">();
  public readonly id = input<string>();
  public readonly initial = input<Partial<MaintenanceRecordFormValue> | null>(null);
  public readonly cancelLink = input<string>("/maintenance/records");

  public readonly save = output<MaintenanceRecordDto>();

  protected readonly model = signal<MaintenanceRecordFormValue>({
    truck: null,
    maintenanceScheduleId: null,
    type: "repair",
    vehicleSystem: null,
    description: "",
    serviceDate: new Date(),
    odometerReading: null,
    engineHours: null,
    requisitionNumber: "",
    vendorName: "",
    invoiceNumber: "",
    laborCost: 0,
    partsCost: 0,
    notes: "",
    parts: [],
  });

  protected readonly hasParts = computed(() => this.model().parts.length > 0);
  protected readonly partsTotal = computed(() =>
    this.model().parts.reduce((sum, p) => sum + (p.quantity ?? 0) * (p.unitCost ?? 0), 0),
  );
  protected readonly total = computed(
    () =>
      (this.model().laborCost ?? 0) +
      (this.hasParts() ? this.partsTotal() : (this.model().partsCost ?? 0)),
  );

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.truck, { message: "Pick the truck." });
      required(p.type, { message: "Pick what kind of service this was." });
      required(p.description, { message: "Say what was done, e.g. Leaf spring replaced." });
      maxLength(p.description, 1000, { message: "Keep it under 1000 characters." });
      required(p.serviceDate, { message: "Pick the service date." });
      maxLength(p.requisitionNumber, 50, { message: "That's longer than a requisition number." });
      min(p.odometerReading, 0, { message: "Can't be negative." });
      min(p.engineHours, 0, { message: "Can't be negative." });
      min(p.laborCost, 0, { message: "Can't be negative." });
      min(p.partsCost, 0, { message: "Can't be negative." });
      applyEach(p.parts, (part) => {
        required(part.partName, { message: "Part?" });
        required(part.quantity, { message: "Qty?" });
        min(part.quantity, 1, { message: "Qty?" });
        required(part.unitCost, { message: "Price?" });
        min(part.unitCost, 0, { message: "Price?" });
      });
    },
    {
      submission: {
        action: async () => {
          const body = this.toCommand();
          const result =
            this.mode() === "create"
              ? await this.api.invoke(createMaintenanceRecord, { body })
              : await this.api.invoke(updateMaintenanceRecord, {
                  id: this.id()!,
                  body: { ...body, id: this.id()! },
                });

          if (result) {
            this.toastService.showSuccess(
              this.mode() === "create" ? "Service record saved" : "Service record updated",
            );
            this.save.emit(result);
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const initial = this.initial();
      if (initial) {
        this.model.update((v) => ({ ...v, ...initial }));
      }
    });
  }

  protected addPart(): void {
    this.model.update((v) => ({ ...v, parts: [...v.parts, emptyPart()] }));
  }

  protected removePart(index: number): void {
    this.model.update((v) => ({ ...v, parts: v.parts.filter((_, i) => i !== index) }));
  }

  protected lineTotal(index: number): number {
    const part = this.model().parts[index];
    return (part?.quantity ?? 0) * (part?.unitCost ?? 0);
  }

  private toCommand(): CreateMaintenanceRecordCommand {
    const value = this.model();
    return {
      truckId: value.truck!.id!,
      maintenanceScheduleId: value.maintenanceScheduleId,
      type: value.type,
      vehicleSystem: value.vehicleSystem ?? undefined,
      description: value.description.trim(),
      serviceDate: value.serviceDate.toISOString(),
      odometerReading: value.odometerReading,
      engineHours: value.engineHours,
      requisitionNumber: value.requisitionNumber.trim() || null,
      vendorName: value.vendorName.trim() || null,
      invoiceNumber: value.invoiceNumber.trim() || null,
      laborCost: value.laborCost ?? 0,
      partsCost: value.parts.length > 0 ? 0 : (value.partsCost ?? 0),
      notes: value.notes.trim() || null,
      parts: value.parts.map((p) => ({
        partName: p.partName.trim(),
        description: p.description.trim() || null,
        quantity: p.quantity ?? 1,
        unitCost: p.unitCost ?? 0,
      })),
    };
  }
}
