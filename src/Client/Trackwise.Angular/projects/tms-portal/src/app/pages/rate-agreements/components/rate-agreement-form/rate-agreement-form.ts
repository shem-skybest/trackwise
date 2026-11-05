import { Component, computed, effect, input, output, signal, untracked } from "@angular/core";
import {
  apply,
  applyEach,
  disabled,
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  required,
  validate,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import type {
  CustomerDto,
  FuelAdjustmentMethod,
  RateAgreementDto,
  RateAgreementLineDto,
  TruckTonnageClass,
} from "@trackwise/shared/api";
import { fuelAdjustmentMethodOptions, truckTonnageClassOptions } from "@trackwise/shared/api/enums";
import {
  Card,
  Grid,
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
import { SearchCustomer } from "@/shared/components/search";
import { dateToIso, isoToDate } from "@/shared/utils";
import {
  emptyTerms,
  rateTermsSchema,
  termsFromDto,
  termsToValue,
  type RateTermsModel,
  type RateTermsValue,
} from "../../utils/rate-terms";
import { ProvinceFuelPercent } from "../province-fuel-percent/province-fuel-percent";
import { RateTermsFields } from "../rate-terms-fields/rate-terms-fields";

/** What the page sends: the create/update command's shape. */
export interface RateAgreementFormValue extends RateTermsValue {
  customerId: string;
  name: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  fuelMethod: FuelAdjustmentMethod;
  notes: string | null;
  lines: RateAgreementLineDto[];
}

type RowKind = "place" | "band";

interface LineRow {
  label: string;
  tonnage: TruckTonnageClass | null;
  city: string;
  province: string;
  minKm: number | null;
  maxKm: number | null;
  amount: number | null;
  fuelPercent: number | null;
  /** Aligned with the form's `tiers`: one amount per diesel price column. */
  surcharges: (number | null)[];
}

interface AgreementModel {
  customer: CustomerDto | null;
  name: string;
  effectiveFrom: Date | null;
  effectiveTo: Date | null;
  fuelMethod: FuelAdjustmentMethod;
  terms: RateTermsModel;
  notes: string;
  rowKind: RowKind;
  tiers: (number | null)[];
  lines: LineRow[];
}

const emptyLine = (tierCount: number): LineRow => ({
  label: "",
  tonnage: null,
  city: "",
  province: "",
  minKm: null,
  maxKm: null,
  amount: null,
  fuelPercent: null,
  surcharges: Array(tierCount).fill(null),
});

/**
 * A rate agreement's terms and its rate card. The card is one row per place (or distance band)
 * and truck size, like the client's Monday board, with a column per diesel tier when fuel is a
 * "gas on top" surcharge.
 */
@Component({
  selector: "app-rate-agreement-form",
  templateUrl: "./rate-agreement-form.html",
  imports: [
    Card,
    FormField,
    FormRoot,
    Grid,
    ProvinceFuelPercent,
    RateTermsFields,
    RouterLink,
    SearchCustomer,
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
export class RateAgreementForm {
  /** Edit mode: an existing draft. The customer is fixed once created. */
  public readonly agreement = input<RateAgreementDto | null>(null);
  public readonly isSaving = input(false);
  public readonly cancelLink = input<string>("/rate-agreements");
  public readonly save = output<RateAgreementFormValue>();

  protected readonly fuelOptions = fuelAdjustmentMethodOptions;
  protected readonly tonnageOptions = [{ label: "Any", value: null }, ...truckTonnageClassOptions];
  protected readonly rowKindOptions = [
    { label: "By town or province", value: "place" },
    { label: "By distance band (km)", value: "band" },
  ];

  protected readonly model = signal<AgreementModel>({
    customer: null,
    name: "",
    effectiveFrom: null,
    effectiveTo: null,
    fuelMethod: "none",
    terms: emptyTerms(),
    notes: "",
    rowKind: "place",
    tiers: [],
    lines: [emptyLine(0)],
  });

  protected readonly isEdit = computed(() => this.agreement() !== null);
  protected readonly isBand = computed(() => this.model().rowKind === "band");
  protected readonly usesTiers = computed(
    () => this.model().fuelMethod === "diesel_tier_surcharge",
  );
  protected readonly usesPercent = computed(() => this.model().fuelMethod === "percentage");

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.customer, { message: "Pick the client." });
      disabled(p.customer, { when: () => this.isEdit() });
      required(p.name, {
        message: "Name it the way the card is known, e.g. HFI rates March 13, 2026.",
      });
      maxLength(p.name, 200, { message: "Keep the name under 200 characters." });
      required(p.effectiveFrom, { message: "Pick the date these rates start." });
      validate(p.effectiveTo, ({ value, valueOf }) => {
        const to = value();
        const from = valueOf(p.effectiveFrom);
        return to && from && to < from
          ? { kind: "range", message: "It can't end before it starts." }
          : null;
      });
      apply(p.terms, rateTermsSchema);
      maxLength(p.notes, 2000, { message: "Keep notes under 2000 characters." });

      applyEach(p.tiers, (tier) => {
        required(tier, { message: "Diesel price?" });
        min(tier, 1, { message: "Diesel price?" });
      });

      applyEach(p.lines, (line) => {
        required(line.label, { message: "Row name?" });
        required(line.amount, { message: "Rate?" });
        min(line.amount, 1, { message: "Rate?" });
        validate(line.city, ({ valueOf }) =>
          this.model().rowKind === "place" &&
          !valueOf(line.city).trim() &&
          !valueOf(line.province).trim()
            ? { kind: "place", message: "Town or province?" }
            : null,
        );
        validate(line.maxKm, ({ valueOf }) => {
          if (this.model().rowKind !== "band") {
            return null;
          }
          const lo = valueOf(line.minKm);
          const hi = valueOf(line.maxKm);
          return lo == null || hi == null || hi < lo
            ? { kind: "band", message: "From-to km?" }
            : null;
        });
      });
    },
    {
      submission: {
        action: async () => {
          this.save.emit(this.toValue());
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => {
      const agreement = this.agreement();
      if (agreement) {
        untracked(() => this.model.set(fromDto(agreement)));
      }
    });
  }

  protected addLine(): void {
    this.model.update((v) => ({ ...v, lines: [...v.lines, emptyLine(v.tiers.length)] }));
  }

  /** A copy of the row below it - the usual next step is the same place for the other truck size. */
  protected duplicateLine(index: number): void {
    this.model.update((v) => {
      const lines = [...v.lines];
      lines.splice(index + 1, 0, {
        ...v.lines[index],
        tonnage: null,
        surcharges: [...v.lines[index].surcharges],
      });
      return { ...v, lines };
    });
  }

  protected setProvinceFuelPercent(province: string, percent: number): void {
    this.model.update((v) => ({
      ...v,
      lines: v.lines.map((l) =>
        l.province.trim() === province ? { ...l, fuelPercent: percent } : l,
      ),
    }));
  }

  protected removeLine(index: number): void {
    this.model.update((v) => ({ ...v, lines: v.lines.filter((_, i) => i !== index) }));
  }

  protected addTier(): void {
    this.model.update((v) => ({
      ...v,
      tiers: [...v.tiers, null],
      lines: v.lines.map((l) => ({ ...l, surcharges: [...l.surcharges, null] })),
    }));
  }

  protected removeTier(index: number): void {
    this.model.update((v) => ({
      ...v,
      tiers: v.tiers.filter((_, i) => i !== index),
      lines: v.lines.map((l) => ({ ...l, surcharges: l.surcharges.filter((_, i) => i !== index) })),
    }));
  }

  private toValue(): RateAgreementFormValue {
    const v = this.model();
    const band = v.rowKind === "band";

    return {
      customerId: v.customer!.id!,
      name: v.name.trim(),
      effectiveFrom: dateToIso(v.effectiveFrom)!,
      effectiveTo: dateToIso(v.effectiveTo),
      fuelMethod: v.fuelMethod,
      ...termsToValue(v.terms),
      notes: v.notes.trim() || null,
      lines: v.lines.map((l) => ({
        label: l.label.trim(),
        tonnage: l.tonnage ?? undefined,
        amount: l.amount ?? 0,
        city: band ? null : l.city.trim() || null,
        province: band ? null : l.province.trim() || null,
        minKm: band ? l.minKm : null,
        maxKm: band ? l.maxKm : null,
        fuelPercent: v.fuelMethod === "percentage" ? l.fuelPercent : null,
        fuelSurcharges:
          v.fuelMethod === "diesel_tier_surcharge"
            ? v.tiers
                .map((tier, i) => ({ dieselPrice: tier, amount: l.surcharges[i] }))
                .filter(
                  (s): s is { dieselPrice: number; amount: number } =>
                    s.dieselPrice != null && s.amount != null,
                )
            : [],
      })),
    };
  }
}

function fromDto(agreement: RateAgreementDto): AgreementModel {
  const lines = agreement.lines ?? [];
  const tiers = [
    ...new Set(lines.flatMap((l) => (l.fuelSurcharges ?? []).map((s) => s.dieselPrice ?? 0))),
  ].sort((a, b) => a - b);

  return {
    customer: { id: agreement.customerId, name: agreement.customerName ?? "" } as CustomerDto,
    name: agreement.name ?? "",
    effectiveFrom: isoToDate(agreement.effectiveFrom),
    effectiveTo: isoToDate(agreement.effectiveTo),
    fuelMethod: agreement.fuelMethod ?? "none",
    terms: termsFromDto(agreement),
    notes: agreement.notes ?? "",
    rowKind: lines.some((l) => l.minKm != null) ? "band" : "place",
    tiers,
    lines: lines.length
      ? lines.map((l) => ({
          label: l.label ?? "",
          tonnage: l.tonnage ?? null,
          city: l.city ?? "",
          province: l.province ?? "",
          minKm: l.minKm ?? null,
          maxKm: l.maxKm ?? null,
          amount: l.amount ?? null,
          fuelPercent: l.fuelPercent ?? null,
          surcharges: tiers.map(
            (t) => l.fuelSurcharges?.find((s) => s.dieselPrice === t)?.amount ?? null,
          ),
        }))
      : [emptyLine(tiers.length)],
  };
}
