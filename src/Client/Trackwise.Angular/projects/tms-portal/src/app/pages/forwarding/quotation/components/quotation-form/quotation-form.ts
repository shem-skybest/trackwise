import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from "@angular/core";
import { form, FormField, FormRoot, maxLength, required } from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { createListStore, EmptyState, getLocations } from "@trackwise/shared";
import type {
  ComplianceRegime,
  CreateQuotationCommand,
  CustomerDto,
  FreightMode,
  Incoterm,
  LocationDto,
  QuotationDto,
  RouteStopSummaryDto,
  ShipmentType,
  TemperatureZone,
  TransportMode,
} from "@trackwise/shared/api";
import {
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiCheckboxField,
  UiFormField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { SearchCustomer } from "@/shared/components/search";
import { RouteStopDialog, type FormModel } from "../add-stop-dialog/add-stop-dialog";

export interface QuotationFormModel {
  customer: CustomerDto | null;
  currency: string;
  incoterm: Incoterm;
  freightMode: FreightMode;
  mode: TransportMode;
  shipmentType: ShipmentType;
  requiresCustomsBrokerage: boolean;
  isHazardous: boolean;

  // Route & Lane
  stops: RouteStopSummaryDto[] | null;
  originLocationId?: string | null;
  destinationLocationId?: string | null;
  requestedPickupWindowStart: string;
  requestedDeliveryWindowEnd: string;
  totalDistanceKm: string;

  // Cargo Specs
  commodity: string;
  palletCount: string;
  totalWeightKg: string;
  declaredCargoValueAmount: string;
  declaredCargoValueCurrency: string;
  requiresActiveRefrigeration: boolean;

  // Temperature Control
  temperatureZone: TemperatureZone;
  setpointCelsius: string;
  allowableVarianceCelsius: string;
  compliance: ComplianceRegime;
  requiresContinuousTelematics: boolean;

  notes: string;
}

const EMPTY_FORM: QuotationFormModel = {
  customer: null,
  currency: "PHP",
  incoterm: "fca",
  freightMode: "domestic",
  mode: "road",
  shipmentType: "ftl",
  requiresCustomsBrokerage: false,
  isHazardous: false,

  stops: null,
  originLocationId: "",
  destinationLocationId: "",
  requestedPickupWindowStart: "",
  requestedDeliveryWindowEnd: "",
  totalDistanceKm: "",

  commodity: "",
  palletCount: "",
  totalWeightKg: "",
  declaredCargoValueAmount: "",
  declaredCargoValueCurrency: "PHP",
  requiresActiveRefrigeration: false,

  temperatureZone: "ambient",
  setpointCelsius: "",
  allowableVarianceCelsius: "",
  compliance: "standard",
  requiresContinuousTelematics: false,

  notes: "",
};

const CustomerLocationsStore = createListStore<LocationDto>(getLocations, {
  defaultSortField: "Name",
  defaultPageSize: 25,
});

const SHIPMENT_TYPES_BY_MODE: Record<TransportMode, { label: string; value: ShipmentType }[]> = {
  road: [
    { label: "FTL – Full Truckload", value: "ftl" },
    { label: "LTL – Less Than Truckload", value: "ltl" },
  ],
  ocean: [
    { label: "FCL – Full Container Load", value: "fcl" },
    { label: "LCL – Less Than Container Load", value: "lcl" },
  ],
  air: [
    { label: "Air Standard", value: "air_standard" },
    { label: "Air Express", value: "air_express" },
    { label: "Air Charter", value: "air_charter" },
  ],
  rail: [
    { label: "Full Wagon Load (FTL)", value: "ftl" },
    { label: "Less Than Wagon Load (LTL)", value: "ltl" },
    { label: "Intermodal Container (FCL)", value: "fcl" },
  ],
  multimodal: [
    { label: "FCL – Container Load", value: "fcl" },
    { label: "LCL – Groupage Load", value: "lcl" },
    { label: "FTL – Full Truckload", value: "ftl" },
    { label: "Air Standard", value: "air_standard" },
  ],
};

@Component({
  selector: "app-quotation-form",
  templateUrl: "./quotation-form.html",
  providers: [CustomerLocationsStore],
  imports: [
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    SearchCustomer,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiCheckboxField,
    UiFormField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
    RouteStopDialog,
    EmptyState,
  ],
})
export class QuotationForm {
  protected readonly selectedCustomerLocationsStore = inject(CustomerLocationsStore);

  public readonly isLoading = input(false);

  /** When provided, the form is pre-filled for editing. */
  public readonly quotation = input<QuotationDto | null>(null);
  public readonly submitLabel = input("Create Quotation");

  public readonly save = output<CreateQuotationCommand>();
  public isAddingRouteStop = signal(false);

  protected readonly model = signal<QuotationFormModel>({ ...EMPTY_FORM });

  private previousFreightMode: FreightMode = "domestic";
  private previousTransportMode: TransportMode = "road";

  // Reactive state derived from form signals
  protected readonly currentFreightMode = computed(() => this.model().freightMode);
  protected readonly currentTransportMode = computed(() => this.model().mode);
  protected readonly isDomestic = computed(() => this.currentFreightMode() === "domestic");
  protected readonly isColdChainRequired = computed(() => this.model().requiresActiveRefrigeration);

  // Dynamic Shipment Types filtered strictly by the chosen Transport Mode
  protected readonly shipmentTypeOptions = computed(() => {
    const mode = this.currentTransportMode() ?? "road";
    return SHIPMENT_TYPES_BY_MODE[mode] ?? SHIPMENT_TYPES_BY_MODE.road;
  });

  // Adaptive Origin / Destination labels
  protected readonly originLocationLabel = computed(() => {
    switch (this.currentTransportMode()) {
      case "ocean":
        return "Port of Loading (POL)";
      case "air":
        return "Airport of Departure (Origin)";
      default:
        return "Origin Pickup Location";
    }
  });

  protected readonly destinationLocationLabel = computed(() => {
    switch (this.currentTransportMode()) {
      case "ocean":
        return "Port of Discharge (POD)";
      case "air":
        return "Airport of Destination (POD)";
      default:
        return "Final Delivery Destination";
    }
  });

  readonly routingModeOptions = [
    { label: "Point-to-Point", value: "point-to-point" },
    { label: "Multi-Route Stops", value: "multi-stop" },
  ];

  readonly routingMode = signal<"point-to-point" | "multi-stop">("point-to-point");

  protected readonly incotermOptions: { label: string; value: Incoterm }[] = [
    { label: "EXW – Ex Works", value: "exw" },
    { label: "FCA – Free Carrier", value: "fca" },
    { label: "FOB – Free on Board (Maritime)", value: "fob" },
    { label: "CIF – Cost, Insurance & Freight", value: "cif" },
    { label: "CPT – Carriage Paid To", value: "cpt" },
    { label: "DAP – Delivered at Place", value: "dap" },
    { label: "DDP – Delivered Duty Paid", value: "ddp" },
  ];

  protected readonly freightModeOptions: { label: string; value: FreightMode }[] = [
    { label: "Domestic", value: "domestic" },
    { label: "Import", value: "import" },
    { label: "Export", value: "export" },
    { label: "Triangular", value: "triangular" },
  ];

  protected readonly transportModeOptions: { label: string; value: TransportMode }[] = [
    { label: "Road Transport", value: "road" },
    { label: "Ocean Freight", value: "ocean" },
    { label: "Air Freight", value: "air" },
    { label: "Rail Freight", value: "rail" },
    { label: "Multimodal Transport", value: "multimodal" },
  ];

  protected readonly temperatureZoneOptions: { label: string; value: TemperatureZone }[] = [
    { label: "Ambient (No Active Cooling)", value: "ambient" },
    { label: "Controlled Room Temperature (15°C to 25°C)", value: "controlled_room_temperature" },
    { label: "Chilled (2°C to 8°C)", value: "chilled" },
    { label: "Frozen (-20°C or colder)", value: "frozen" },
    { label: "Cryogenic (Deep Freeze)", value: "cryogenic" },
  ];

  protected readonly complianceRegimeOptions: { label: string; value: ComplianceRegime }[] = [
    { label: "Standard Logistics", value: "standard" },
    { label: "GDP (Good Distribution Practice - Pharma)", value: "gdp" },
    { label: "FSMA (Food Safety Modernization)", value: "fsma" },
    { label: "HACCP (Hazard Analysis Critical Control Point)", value: "haccp" },
  ];

  protected readonly selectedCustomerLocationOptions = computed(() =>
    this.selectedCustomerLocationsStore.data().map((loc) => ({
      value: loc.id ?? "",
      label: loc.code ? `${loc.code} — ${loc.name}` : (loc.name ?? ""),
    })),
  );

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.customer, { message: "Customer is required." });
      required(p.currency, { message: "Currency is required." });
      maxLength(p.currency, 3, { message: "Currency must be a 3-letter ISO code." });
      required(p.freightMode, { message: "Freight mode is required." });
      required(p.mode, { message: "Transport mode is required." });
      required(p.shipmentType, { message: "Shipment type is required." });
      required(p.incoterm, { message: "Incoterm is required." });
    },
    {
      submission: {
        action: async () => {
          this.save.emit(this.toCommand());
          return undefined;
        },
      },
    },
  );

  constructor() {
    // 0. Edit mode -> hydrate the form from an existing quotation
    effect(() => {
      const q = this.quotation();
      // console.log(q);
      if (q) {
        untracked(() => this.patchFromQuotation(q));
      }
    });

    // 1. Customer location query sync
    effect(() => {
      const customerId = this.model().customer?.id;
      untracked(() => {
        this.selectedCustomerLocationsStore.setFilters(
          { CustomerId: customerId },
          { reload: true },
        );
      });
    });

    // 2. Freight Mode watcher -> Auto-populate presets
    effect(() => {
      const mode = this.currentFreightMode();
      if (mode && mode !== this.previousFreightMode) {
        this.previousFreightMode = mode;
        untracked(() => this.handleFreightModeChange(mode));
      }
    });

    // 3. Transport Mode watcher -> Auto-correct Shipment Type
    effect(() => {
      const mode = this.currentTransportMode();
      if (mode && mode !== this.previousTransportMode) {
        this.previousTransportMode = mode;
        untracked(() => this.handleTransportModeChange(mode));
      }
    });
  }

  /**
   * Maps the flat QuotationDto back into the form model.
   * Fields the DTO doesn't carry are derived or defaulted (see inline notes).
   */
  private patchFromQuotation(q: QuotationDto): void {
    const freightMode = q.freightMode ?? EMPTY_FORM.freightMode;
    const mode = q.mode ?? EMPTY_FORM.mode;
    const currency = q.currency ?? EMPTY_FORM.currency;

    // IMPORTANT: sync these first, otherwise the freight/transport mode
    // watchers fire and overwrite incoterm, currency, mode and shipmentType
    // with their presets.
    this.previousFreightMode = freightMode;
    this.previousTransportMode = mode;

    // Decide routing mode from the stops. A plain pickup -> dropoff route is
    // treated as point-to-point, anything else as multi-stop.
    const stops = [...(q.stops ?? [])].sort((a, b) => (a.sequence ?? 0) - (b.sequence ?? 0));
    const first = stops[0];
    const last = stops[stops.length - 1];
    const isPointToPoint =
      stops.length === 2 && first?.type === "pickup" && last?.type === "dropoff";

    this.routingMode.set(stops.length > 0 && !isPointToPoint ? "multi-stop" : "point-to-point");

    const zone = q.temperatureSpec?.zone ?? EMPTY_FORM.temperatureZone;

    this.model.set({
      ...EMPTY_FORM,
      customer: q.customerId
        ? ({ id: q.customerId, name: q.customerName ?? "" } as CustomerDto)
        : null,
      currency,
      incoterm: q.incoterm ?? EMPTY_FORM.incoterm,
      freightMode,
      mode,
      shipmentType: q.shipmentType ?? EMPTY_FORM.shipmentType,
      requiresCustomsBrokerage: q.requiresCustomsBrokerage!,
      isHazardous: q.isHazardous!,

      stops: stops.length > 0 && !isPointToPoint ? stops : null,
      originLocationId: isPointToPoint ? (first?.locationId ?? "") : "",
      destinationLocationId: isPointToPoint ? (last?.locationId ?? "") : "",
      requestedPickupWindowStart: first?.windowStart ?? "",
      requestedDeliveryWindowEnd: last?.windowEnd ?? "",
      totalDistanceKm: q.totalDistanceKm?.toString() ?? "",

      commodity: q.commodity ?? "",
      palletCount: q.palletCount?.toString() ?? "",
      totalWeightKg: q.totalWeightKg?.toString() ?? "",
      declaredCargoValueAmount: q.declaredCargoValueAmount?.toString() ?? "",
      declaredCargoValueCurrency: q.declaredCargoValueCurrency ?? "",
      requiresActiveRefrigeration: !!q.temperatureSpec?.zone && zone !== "ambient",

      temperatureZone: zone,
      setpointCelsius: q.temperatureSpec?.setpointCelsius?.toString() ?? "",
      allowableVarianceCelsius: q.temperatureSpec?.allowableVarianceCelsius?.toString() ?? "",
      compliance: q.temperatureSpec?.compliance ?? EMPTY_FORM.compliance,
      requiresContinuousTelematics: q.temperatureSpec?.requiresContinuousTelematics ?? false,

      notes: q.notes ?? "",
    });
    // console.log(this.model());
    // console.log(q);
  }

  private handleFreightModeChange(freightMode: FreightMode): void {
    this.model.update((curr) => {
      if (freightMode === "domestic") {
        return {
          ...curr,
          currency: "PHP",
          declaredCargoValueCurrency: "PHP",
          requiresCustomsBrokerage: false,
          incoterm: "fca",
          mode: "road",
          shipmentType: "ftl",
        };
      }

      // Import / Export / Triangular
      return {
        ...curr,
        currency: curr.currency === "PHP" ? "USD" : curr.currency,
        declaredCargoValueCurrency:
          curr.declaredCargoValueCurrency === "PHP" ? "USD" : curr.declaredCargoValueCurrency,
        requiresCustomsBrokerage: true,
        incoterm: freightMode === "import" ? "cif" : "fob",
        mode: curr.mode === "road" ? "ocean" : curr.mode,
        shipmentType: curr.mode === "road" ? "fcl" : curr.shipmentType,
      };
    });
  }

  private handleTransportModeChange(transportMode: TransportMode): void {
    const validShipmentTypes = SHIPMENT_TYPES_BY_MODE[transportMode].map((opt) => opt.value);
    this.model.update((curr) => {
      if (validShipmentTypes.includes(curr.shipmentType)) {
        return curr;
      }
      return {
        ...curr,
        shipmentType: validShipmentTypes[0],
      };
    });
  }

  protected toggleColdChain(enabled: boolean): void {
    this.model.update((curr) => ({
      ...curr,
      requiresActiveRefrigeration: enabled,
      temperatureZone:
        enabled && curr.temperatureZone === "ambient" ? "chilled" : curr.temperatureZone,
    }));
  }

  private parseOptionalNumber(val: string | null | undefined): number | undefined {
    if (!val) return undefined;
    const trimmed = val.trim();
    if (!trimmed) return undefined;
    const parsed = Number(trimmed);
    return isNaN(parsed) ? undefined : parsed;
  }

  private toCommand(): CreateQuotationCommand {
    const v = this.model();
    const distance = this.parseOptionalNumber(v.totalDistanceKm);
    const setpoint = this.parseOptionalNumber(v.setpointCelsius);
    const variance = this.parseOptionalNumber(v.allowableVarianceCelsius);

    const hasCargoData = Boolean(
      v.commodity?.trim() || v.totalWeightKg || v.palletCount || v.declaredCargoValueAmount,
    );

    const command: CreateQuotationCommand = {
      customerId: v.customer?.id ?? "",
      currency: v.currency.trim().toUpperCase(),
      freightMode: v.freightMode,
      mode: v.mode,
      shipmentType: v.shipmentType,
      incoterm: v.incoterm,
      isHazardous: v.isHazardous,
      requiresCustomsBrokerage: v.requiresCustomsBrokerage,
      notes: v.notes.trim() || null,
      lane: {
        stops: v.stops,
        originLocationId: v.originLocationId?.trim() || null,
        destinationLocationId: v.destinationLocationId?.trim() || null,
        requestedPickupWindowStart: v.requestedPickupWindowStart.trim() || null,
        requestedDeliveryWindowEnd: v.requestedDeliveryWindowEnd.trim() || null,
        totalDistanceKm: distance !== undefined ? distance : null,
      },
      cargo: hasCargoData
        ? {
            commodity: v.commodity?.trim() || null,
            palletCount: this.parseOptionalNumber(v.palletCount),
            totalWeightKg: this.parseOptionalNumber(v.totalWeightKg),
            declaredCargoValueAmount: this.parseOptionalNumber(v.declaredCargoValueAmount),
            declaredCargoValueCurrency:
              v.declaredCargoValueCurrency?.trim().toUpperCase() || v.currency.trim().toUpperCase(),
            requiresActiveRefrigeration: Boolean(v.requiresActiveRefrigeration),
          }
        : undefined,
    };

    if (this.isColdChainRequired() || setpoint !== undefined) {
      command.temperatureSpec = {
        zone: v.temperatureZone,
        setpointCelsius: setpoint,
        allowableVarianceCelsius: variance,
        compliance: v.compliance,
        requiresContinuousTelematics: v.requiresContinuousTelematics,
      };
    }

    return command;
  }

  protected readonly stopTypeLabels: Record<string, string> = {
    pickup: "Pickup Location",
    dropoff: "Delivery Destination",
    waypoint: "Waypoint Stop",
    cross_dock_staging: "Crossdocking Staging",
  };

  protected handleAddStop(newStop: FormModel): void {
    const selectedLoc = this.selectedCustomerLocationsStore
      .data()
      .find((loc) => loc.id === newStop.locationId);

    const currentStops = this.model().stops ?? [];
    const stop: RouteStopSummaryDto = {
      sequence: currentStops.length + 1,
      type: newStop.stopType as RouteStopSummaryDto["type"],
      locationId: newStop.locationId,
      locationName: selectedLoc?.name ?? "",
      locationCode: selectedLoc?.code ?? null,
      windowStart: null,
      windowEnd: null,
    };

    this.model.update((curr) => ({
      ...curr,
      stops: [...(curr.stops ?? []), stop],
    }));
  }

  handleRemoveStop(stop: RouteStopSummaryDto): void {
    this.model.update((current) => ({
      ...current,
      stops: (current.stops ?? [])
        .filter((s) => s.sequence !== stop.sequence)
        .map((s, index) => ({
          ...s,
          sequence: index + 1,
        })),
    }));
  }
}
