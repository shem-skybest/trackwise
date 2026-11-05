import { Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { form, FormField, FormRoot, min, required } from "@angular/forms/signals";
import { Router, RouterLink } from "@angular/router";
import {
  addTariffRate,
  Api,
  getChargeCodes,
  type AddTariffRateCommand,
  type ChargeCodeDto,
  type PricingModel,
} from "@trackwise/shared/api";
import {
  Card,
  Container,
  Grid,
  Icon,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";

interface TariffRateFormModel {
  chargeCodeId: string | null;
  pricingModel: PricingModel | null;
  baseAmount: number;
  unit: string;
  minimum: number | null;
  maximum: number | null;
  freeDays: number | null;
  rateAfterFreeDays: number | null;
  percentage: number | null;
  percentageBase: string | null;
  customPercentageBase: string;
  breakpointJson: string;
}

const EMPTY: TariffRateFormModel = {
  chargeCodeId: null,
  pricingModel: "flat",
  baseAmount: 0,
  unit: "per shipment",
  minimum: null,
  maximum: null,
  freeDays: null,
  rateAfterFreeDays: null,
  percentage: null,
  percentageBase: "BAS",
  customPercentageBase: "",
  breakpointJson: "",
};

@Component({
  selector: "app-tariff-rate-add",
  templateUrl: "./tariff-rate-add.html",
  imports: [
    Card,
    Container,
    FormField,
    FormRoot,
    Grid,
    Icon,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    Typography,
    UiButton,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class TariffRateAdd {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  public readonly id = input.required<string>();
  protected readonly chargeCodes = signal<ChargeCodeDto[]>([]);
  protected readonly isLoading = signal(true);

  protected readonly pricingModels = [
    { label: "Flat Fee", value: "flat" as PricingModel },
    { label: "Per Unit", value: "per_unit" as PricingModel },
    { label: "Percentage", value: "percentage" as PricingModel },
    { label: "Breakpoint / Tiered", value: "breakpoint" as PricingModel },
  ];

  protected readonly percentageBases = [
    { label: "BAS – Base Freight (Derived from quote)", value: "BAS" },
    { label: "CIF – Cost, Insurance & Freight (Derived from quote)", value: "CIF" },
    { label: "FOB – Free On Board", value: "FOB" },
    { label: "Custom Valuation Base", value: "CUSTOM" },
  ];

  protected readonly chargeCodeOptions = computed(() =>
    this.chargeCodes().map((ch) => ({
      value: ch.id,
      label: ch.name ? `${ch.code} - ${ch.name}` : (ch.code ?? ""),
    })),
  );

  protected readonly model = signal<TariffRateFormModel>({ ...EMPTY });
  private previousPricingModel: PricingModel | null = "flat";

  protected readonly selectedChargeCodeId = computed(() => this.model().chargeCodeId);
  protected readonly currentPricingModel = computed(() => this.model().pricingModel);

  protected readonly percentageBaseHint = computed(() => {
    const base = this.model().percentageBase;
    switch (base) {
      case "BAS":
        return "Calculated against core base freight. Automatically derived by the quoting engine.";
      case "CIF":
        return "Calculated against total landed cargo value (Cost + Insurance + Freight). Automatically derived.";
      case "CUSTOM":
        return "Explicit valuation base required during quotation.";
      default:
        return "Base amount used to calculate the percentage charge.";
    }
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.chargeCodeId, { message: "Charge code is required." });
      required(p.pricingModel, { message: "Pricing model is required." });
      required(p.unit, { message: "Unit is required." });

      const model = this.model().pricingModel;
      if (model === "percentage") {
        required(p.percentage, { message: "Percentage rate is required." });
        min(p.percentage, 0, { message: "Percentage rate cannot be negative." });
        required(p.percentageBase, { message: "Percentage base is required." });
      } else if (model === "breakpoint") {
        required(p.breakpointJson, { message: "Breakpoint definition is required." });
      } else {
        required(p.baseAmount, { message: "Base amount is required." });
        min(p.baseAmount, 0, { message: "Base amount cannot be negative." });
      }
    },
    {
      submission: {
        action: async () => {
          const value = this.model();
          const isPercentage = value.pricingModel === "percentage";
          const isBreakpoint = value.pricingModel === "breakpoint";
          const isFlat = value.pricingModel === "flat";

          const resolvedPercentageBase = isPercentage
            ? value.percentageBase === "CUSTOM"
              ? value.customPercentageBase.trim() || null
              : value.percentageBase
            : null;

          const command: AddTariffRateCommand = {
            chargeCodeId: value.chargeCodeId!,
            pricingModel: value.pricingModel!,
            baseAmount: isPercentage ? 0 : Number(value.baseAmount ?? 0),
            unit: value.unit.trim(),
            minimum: isFlat ? null : value.minimum,
            maximum: isFlat ? null : value.maximum,
            freeDays: isPercentage || isFlat ? null : value.freeDays,
            rateAfterFreeDays: isPercentage || isFlat ? null : value.rateAfterFreeDays,
            percentage: isPercentage ? value.percentage : null,
            percentageBase: resolvedPercentageBase,
            breakpointJson: isBreakpoint ? value.breakpointJson.trim() || null : null,
          };

          try {
            await this.api.invoke(addTariffRate, { id: this.id(), body: command });
            this.toastService.showSuccess("Rate added to the tariff");
            await this.router.navigate(["/tariffs", this.id()]);
          } catch {
            this.toastService.showError("Failed to add rate");
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    // React to Charge Code selection
    effect(() => {
      const chargeCodeId = this.selectedChargeCodeId();
      if (!chargeCodeId) return;

      untracked(() => {
        const chargeCode = this.chargeCodes().find((item) => item.id === chargeCodeId);
        if (chargeCode?.defaultUnit && this.model().pricingModel !== "percentage") {
          this.model.update((current) => ({
            ...current,
            unit: chargeCode.defaultUnit!,
          }));
        }
      });
    });

    // React to Pricing Model changes to auto-populate defaults
    effect(() => {
      const current = this.currentPricingModel();
      if (current && current !== this.previousPricingModel) {
        const prev = this.previousPricingModel;
        this.previousPricingModel = current;

        if (prev !== null) {
          untracked(() => this.handlePricingModelChange(current));
        }
      }
    });

    void this.loadChargeCodes();
  }

  private handlePricingModelChange(model: PricingModel): void {
    const chargeCode = this.chargeCodes().find((item) => item.id === this.model().chargeCodeId);

    this.model.update((current) => {
      switch (model) {
        case "percentage":
          return {
            ...current,
            unit: chargeCode?.defaultUnit ?? "%",
            percentage: current.percentage ?? 0,
            percentageBase: current.percentageBase || "BAS",
            baseAmount: 0,
          };
        case "flat":
          return {
            ...current,
            unit: chargeCode?.defaultUnit || "per shipment",
          };
        case "per_unit":
          return {
            ...current,
            unit: chargeCode?.defaultUnit || "per kg",
          };
        case "breakpoint":
          return {
            ...current,
            unit: chargeCode?.defaultUnit || "per kg",
          };
        default:
          return current;
      }
    });
  }

  protected insertSampleBreakpoint(): void {
    const sample = JSON.stringify(
      [
        { from: 0, to: 100, rate: 25 },
        { from: 101, to: 500, rate: 20 },
        { from: 501, rate: 15 },
      ],
      null,
      2,
    );
    this.model.update((val) => ({ ...val, breakpointJson: sample }));
  }

  private async loadChargeCodes(): Promise<void> {
    try {
      this.chargeCodes.set((await this.api.invoke(getChargeCodes, { OnlyActive: true })) ?? []);
    } finally {
      this.isLoading.set(false);
    }
  }
}
