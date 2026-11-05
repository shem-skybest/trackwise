import { DecimalPipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { form, FormField } from "@angular/forms/signals";
import {
  Api,
  costRoute,
  type RouteCostingDto,
  type TruckTonnageClass,
} from "@trackwise/shared/api";
import {
  Card,
  Grid,
  Stack,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiToggleField,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { dateToIso } from "@/shared/utils";

type Assumption =
  | "kmPerLitre"
  | "driverDayRate"
  | "helperDayRate"
  | "otherCrewCost"
  | "driverAllowance"
  | "helperAllowance"
  | "adminPercent"
  | "markupPercent"
  | "buffer"
  | "roundUpTo";

interface CostingModel {
  truckSize: TruckTonnageClass;
  garageToWarehouseKm: number | null;
  warehouseToDropKm: number | null;
  dropToGarageKm: number | null;
  seniorDriver: boolean;
  dieselPricePerLitre: number | null;
  assumptions: Record<Assumption, number | null>;
}

const ASSUMPTION_KEYS: Assumption[] = [
  "kmPerLitre",
  "driverDayRate",
  "helperDayRate",
  "otherCrewCost",
  "driverAllowance",
  "helperAllowance",
  "adminPercent",
  "markupPercent",
  "buffer",
  "roundUpTo",
];

const noOverrides = (): Record<Assumption, number | null> =>
  Object.fromEntries(ASSUMPTION_KEYS.map((k) => [k, null])) as Record<Assumption, number | null>;

/**
 * Cold Chain's costing board as a calculator: what a route should be quoted at from its km, the
 * truck size and the diesel price. Blank assumptions take the size's defaults; changing the size or
 * the driver's seniority resets them.
 */
@Component({
  selector: "app-route-costing",
  templateUrl: "./route-costing.html",
  imports: [
    Card,
    DecimalPipe,
    FormField,
    Grid,
    PageHeader,
    Stack,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiToggleField,
  ],
})
export class RouteCosting {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  protected readonly sizeOptions = [
    { label: "2 Tonner", value: "two_tonner" },
    { label: "4 Tonner", value: "four_tonner" },
  ];

  protected readonly assumptionFields: {
    key: Assumption;
    label: string;
    prefix?: string;
    suffix?: string;
  }[] = [
    { key: "kmPerLitre", label: "Km per litre", suffix: "km/L" },
    { key: "driverDayRate", label: "Driver day rate", prefix: "₱" },
    { key: "helperDayRate", label: "Helper day rate", prefix: "₱" },
    { key: "otherCrewCost", label: "Other crew cost", prefix: "₱" },
    { key: "driverAllowance", label: "Driver allowance", prefix: "₱" },
    { key: "helperAllowance", label: "Helper allowance", prefix: "₱" },
    { key: "adminPercent", label: "Admin", suffix: "%" },
    { key: "markupPercent", label: "Markup", suffix: "%" },
    { key: "buffer", label: "Buffer", prefix: "₱" },
    { key: "roundUpTo", label: "Round up to", prefix: "₱" },
  ];

  protected readonly model = signal<CostingModel>({
    truckSize: "two_tonner",
    garageToWarehouseKm: null,
    warehouseToDropKm: null,
    dropToGarageKm: null,
    seniorDriver: true,
    dieselPricePerLitre: null,
    assumptions: noOverrides(),
  });

  protected readonly form = form(this.model);
  protected readonly result = signal<RouteCostingDto | null>(null);
  protected readonly isBusy = signal(false);

  protected resetAssumptions(): void {
    this.model.update((m) => ({ ...m, assumptions: noOverrides() }));
    this.result.set(null);
  }

  protected async compute(): Promise<void> {
    const m = this.model();
    const a = m.assumptions;
    this.isBusy.set(true);
    try {
      const result = await this.api.invoke(costRoute, {
        TruckSize: m.truckSize,
        GarageToWarehouseKm: m.garageToWarehouseKm ?? 0,
        WarehouseToDropKm: m.warehouseToDropKm ?? 0,
        DropToGarageKm: m.dropToGarageKm ?? 0,
        SeniorDriver: m.seniorDriver,
        On: dateToIso(new Date()) ?? undefined,
        DieselPricePerLitre: m.dieselPricePerLitre ?? undefined,
        KmPerLitre: a.kmPerLitre ?? undefined,
        DriverDayRate: a.driverDayRate ?? undefined,
        HelperDayRate: a.helperDayRate ?? undefined,
        OtherCrewCost: a.otherCrewCost ?? undefined,
        DriverAllowance: a.driverAllowance ?? undefined,
        HelperAllowance: a.helperAllowance ?? undefined,
        AdminPercent: a.adminPercent ?? undefined,
        MarkupPercent: a.markupPercent ?? undefined,
        Buffer: a.buffer ?? undefined,
        RoundUpTo: a.roundUpTo ?? undefined,
      });

      this.result.set(result);
      this.model.update((v) => ({
        ...v,
        dieselPricePerLitre: result.dieselPricePerLitre ?? null,
        assumptions: Object.fromEntries(
          ASSUMPTION_KEYS.map((k) => [k, result[k] ?? null]),
        ) as Record<Assumption, number | null>,
      }));
    } catch (error) {
      const message = (error as { error?: { error?: string } })?.error?.error;
      this.toast.showError(message ?? "Could not cost the route.");
    } finally {
      this.isBusy.set(false);
    }
  }
}
