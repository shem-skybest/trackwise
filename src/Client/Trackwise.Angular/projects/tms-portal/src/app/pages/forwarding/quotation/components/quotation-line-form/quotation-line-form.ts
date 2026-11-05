import { UpperCasePipe } from "@angular/common";
import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  type OnInit,
} from "@angular/core";
import { FormsModule } from "@angular/forms";
import {
  Api,
  getChargeCodes,
  getTariffById,
  getTariffs,
  type AddQuotationLineCommand,
  type ChargeCategory,
  type ChargeCodeDto,
  type MarkupType,
  type OperationalServiceType,
  type QuotationDto,
  type TariffDto,
  type TariffRateDto,
} from "@trackwise/shared/api";
import { markupTypeOptions } from "@trackwise/shared/api/enums";
import {
  Alert,
  Grid,
  Icon,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
} from "@trackwise/shared/ui";

export type QuotationLineFormValue = Omit<AddQuotationLineCommand, "quotationId">;

export interface EditableQuotationLine {
  id: string;
  tariffRateId: string;
  chargeCodeId: string;
  chargeCode: string;
  chargeName: string;
  category: ChargeCategory;
  pricingModel: string;
  percentage: number | null;
  percentageBase: string | null;
  unit: string | null;
  tariffBaseAmount: number | null;
  baseAmount: number | null; // Explicit valuation or override
  serviceType: OperationalServiceType | null;
  quantity: number | null;
  markupType: MarkupType | null;
  markupValue: number | null;
  fxRate: number | null;
  notes: string;
}

@Component({
  selector: "app-quotation-line-form",
  templateUrl: "./quotation-line-form.html",
  imports: [
    Alert,
    FormsModule,
    Grid,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
    UpperCasePipe,
    Icon,
  ],
})
export class QuotationLineForm {
  private readonly api = inject(Api);

  public readonly save = output<QuotationLineFormValue[]>();
  public readonly dismiss = output<void>();
  public readonly quotation = input<QuotationDto | null>(null);

  protected readonly tariffs = signal<TariffDto[]>([]);
  protected readonly chargeCodesById = signal<Map<string, ChargeCodeDto>>(new Map());
  protected readonly selectedTariffRates = signal<TariffRateDto[]>([]);
  protected readonly selectedTariffId = signal<string | null>(null);
  protected readonly selectedAdditionalRateId = signal<string | null>(null);
  protected readonly isLoadingTariff = signal<boolean>(false);

  protected readonly lines = signal<EditableQuotationLine[]>([]);

  protected readonly bulkMarkupType = signal<MarkupType | null>("percentage");
  protected readonly bulkMarkupValue = signal<number | null>(0);

  protected readonly serviceTypeOptions: { label: string; value: OperationalServiceType }[] = [
    { label: "Standard", value: "standard" },
    { label: "Line Haul", value: "line_haul" },
    { label: "Pre-Cooling & PTI", value: "pre_cooling_and_pti" },
    { label: "Genset Fuel", value: "genset_fuel" },
    { label: "Continuous Telematics", value: "continuous_telematics" },
    { label: "Cold Storage Plugin", value: "cold_storage_plugin" },
    { label: "Terminal Handling (Origin)", value: "terminal_handling_origin" },
    { label: "Terminal Handling (Destination)", value: "terminal_handling_destination" },
    { label: "Customs Clearance (Export)", value: "customs_clearance_export" },
    { label: "Customs Clearance (Import)", value: "customs_clearance_import" },
    { label: "Bunker Adjustment Factor (BAF)", value: "bunker_adjustment_factor" },
    { label: "Security Filing (ISF)", value: "security_filing_isf" },
    { label: "Demurrage & Detention", value: "demurrage_detention" },
    { label: "Liftgate Pickup/Delivery", value: "liftgate_pickup_delivery" },
    { label: "HAZMAT Surcharge", value: "hazmat_surcharge" },
  ];

  protected readonly markupTypes = markupTypeOptions;

  constructor() {
    effect(() => {
      const quotation = this.quotation();

      if (!quotation) {
        return;
      }

      void this.loadData(quotation);
    });
  }

  readonly tariffOptions = computed(() =>
    this.tariffs().map((tariff) => ({
      value: tariff.id,
      label: tariff.name,
    })),
  );

  protected readonly availableRateOptions = computed(() => {
    const codes = this.chargeCodesById();
    return this.selectedTariffRates().map((r) => {
      const code = codes.get(r.chargeCodeId ?? "");
      const labelPrefix = code?.code ? `${code.code} — ` : "";
      const rateSummary =
        r.pricingModel === "percentage"
          ? `${r.percentage}% ${r.percentageBase ?? "BAS"}`
          : `${r.baseAmount} / ${r.unit ?? "unit"}`;

      return {
        label: `${labelPrefix}${code?.name ?? "Unknown Charge"} (${rateSummary})`,
        value: r.id ?? "",
      };
    });
  });

  protected readonly isValid = computed(() => {
    const currentLines = this.lines();
    if (currentLines.length === 0) return false;

    return currentLines.every((l) => {
      const hasBaseValidity =
        Boolean(l.tariffRateId) &&
        l.serviceType != null &&
        l.quantity != null &&
        l.quantity > 0 &&
        l.markupType != null &&
        l.markupValue != null &&
        l.markupValue >= 0;

      if (!hasBaseValidity) return false;

      // When pricing is percentage and not BAS or CIF, baseAmount is strictly required
      const isCustomPercentage =
        l.pricingModel?.toLowerCase() === "percentage" &&
        l.percentageBase !== "BAS" &&
        l.percentageBase !== "CIF";

      if (isCustomPercentage) {
        return l.baseAmount != null && l.baseAmount > 0;
      }

      return true;
    });
  });

  private async loadData(quotation: QuotationDto): Promise<void> {
    const [tariffsResult, chargeCodes] = await Promise.all([
      this.api.invoke(getTariffs, {
        Mode: quotation.freightMode,
        OnlyActive: true,
        PageSize: 100,
      }),
      this.api.invoke(getChargeCodes, { OnlyActive: true }),
    ]);

    this.tariffs.set(tariffsResult.items ?? []);
    this.chargeCodesById.set(new Map(chargeCodes.map((c) => [c.id ?? "", c])));
  }

  async onTariffChange(tariffId: string | null): Promise<void> {
    this.selectedTariffId.set(tariffId);
    if (!tariffId) {
      this.selectedTariffRates.set([]);
      this.lines.set([]);
      return;
    }

    this.isLoadingTariff.set(true);
    try {
      const tariff = await this.api.invoke(getTariffById, { id: tariffId });
      const rates = tariff.rates ?? [];
      this.selectedTariffRates.set(rates);

      const codes = this.chargeCodesById();
      const initialLines: EditableQuotationLine[] = rates.map((rate) => {
        const code = codes.get(rate.chargeCodeId ?? "");
        return this.createEditableLine(rate, code);
      });

      this.lines.set(initialLines);
    } finally {
      this.isLoadingTariff.set(false);
    }
  }

  addRateLine(rateId: string | null): void {
    if (!rateId) return;
    const rate = this.selectedTariffRates().find((r) => r.id === rateId);
    if (!rate) return;

    const code = this.chargeCodesById().get(rate.chargeCodeId ?? "");
    const newLine = this.createEditableLine(rate, code);

    this.lines.update((items) => [...items, newLine]);
    this.selectedAdditionalRateId.set(null);
  }

  private createEditableLine(rate: TariffRateDto, code?: ChargeCodeDto): EditableQuotationLine {
    const isPercentage = rate.pricingModel?.toLowerCase() === "percentage";
    const isCustomPercentage =
      isPercentage && rate.percentageBase !== "BAS" && rate.percentageBase !== "CIF";

    return {
      id: crypto.randomUUID(),
      tariffRateId: rate.id ?? "",
      chargeCodeId: rate.chargeCodeId ?? "",
      chargeCode: code?.code ?? "",
      chargeName: code?.name ?? "Unknown Charge",
      category: code?.category ?? "base",
      pricingModel: rate.pricingModel ?? "flat",
      percentage: rate.percentage ?? null,
      percentageBase: rate.percentageBase ?? null,
      unit: rate.unit ?? null,
      tariffBaseAmount: rate.baseAmount ?? null,
      baseAmount: isCustomPercentage ? null : isPercentage ? null : (rate.baseAmount ?? null),
      serviceType: this.suggestServiceType(code),
      quantity: 1,
      markupType: this.bulkMarkupType(),
      markupValue: this.bulkMarkupValue(),
      fxRate: null,
      notes: "",
    };
  }

  private suggestServiceType(code?: ChargeCodeDto): OperationalServiceType {
    if (!code) return "standard";
    const text = `${code.code ?? ""} ${code.name ?? ""} ${code.description ?? ""}`.toLowerCase();

    if (text.includes("baf") || text.includes("bunker")) return "bunker_adjustment_factor";
    if (text.includes("isf") || text.includes("security")) return "security_filing_isf";
    if (text.includes("customs") && (text.includes("exp") || text.includes("export")))
      return "customs_clearance_export";
    if (text.includes("customs")) return "customs_clearance_import";
    if (text.includes("thc") || text.includes("terminal")) {
      return text.includes("dest") || text.includes("pod")
        ? "terminal_handling_destination"
        : "terminal_handling_origin";
    }
    if (text.includes("demurrage") || text.includes("detention")) return "demurrage_detention";
    if (text.includes("liftgate") || text.includes("tailgate")) return "liftgate_pickup_delivery";
    if (text.includes("telematics") || text.includes("tracking") || text.includes("iot"))
      return "continuous_telematics";
    if (text.includes("genset")) return "genset_fuel";
    if (text.includes("pre-cooling") || text.includes("pti")) return "pre_cooling_and_pti";
    if (text.includes("cold storage") || text.includes("plug")) return "cold_storage_plugin";
    if (text.includes("line haul") || text.includes("freight")) return "line_haul";

    return "standard";
  }

  removeLine(id: string): void {
    this.lines.update((items) => items.filter((l) => l.id !== id));
  }

  updateLine<K extends keyof EditableQuotationLine>(
    id: string,
    key: K,
    value: EditableQuotationLine[K],
  ): void {
    this.lines.update((items) =>
      items.map((line) => (line.id === id ? { ...line, [key]: value } : line)),
    );
  }

  applyBulkMarkup(): void {
    const type = this.bulkMarkupType() ?? "percentage";
    const value = this.bulkMarkupValue() ?? 0;
    this.lines.update((items) =>
      items.map((line) => ({ ...line, markupType: type, markupValue: value })),
    );
  }

  onSubmit(): void {
    if (!this.isValid()) return;

    const values: QuotationLineFormValue[] = this.lines().map((line) => {
      let finalBaseAmount: number | null = line.baseAmount;

      // If user left baseAmount empty for flat/per_unit, fallback to the tariff's original baseAmount
      if (finalBaseAmount == null && line.pricingModel.toLowerCase() !== "percentage") {
        finalBaseAmount = line.tariffBaseAmount;
      }

      return {
        tariffRateId: line.tariffRateId,
        serviceType: line.serviceType!,
        quantity: line.quantity!,
        baseAmount: finalBaseAmount,
        markupType: line.markupType!,
        markupValue: line.markupValue!,
        fxRate: line.fxRate,
        notes: line.notes.trim() || null,
      };
    });

    this.save.emit(values);
  }
}
