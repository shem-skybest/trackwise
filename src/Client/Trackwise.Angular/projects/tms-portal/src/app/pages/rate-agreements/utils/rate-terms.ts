import { max, min, schema } from "@angular/forms/signals";
import type {
  RateAgreementDto,
  RateExtraDropChargeDto,
  TruckTonnageClass,
} from "@trackwise/shared/api";
import { tonnageLabel } from "./rate-agreement-labels";

type ExtraDropSize = TruckTonnageClass | "any";
type TollsChoice = "unknown" | "at_cost" | "included";

/** The notes under a quotation's rate table, as the form edits them. Blank = the quote doesn't say. */
export interface RateTermsModel {
  includedDrops: number | null;
  extraDrop: Record<ExtraDropSize, number | null>;
  foulTripPercent: number | null;
  backloadPercent: number | null;
  demurrageFreeHours: number | null;
  demurrageHourlyRate: number | null;
  otherLocationCharge: number | null;
  tolls: TollsChoice;
  billExtraDrops: boolean;
  ratesIncludeVat: boolean;
}

/** The terms fields of the create/update command. */
export type RateTermsValue = Pick<
  RateAgreementDto,
  | "includedDrops"
  | "extraDropCharges"
  | "foulTripPercent"
  | "backloadPercent"
  | "demurrageFreeHours"
  | "demurrageHourlyRate"
  | "otherLocationCharge"
  | "tollsAndParkingAtCost"
  | "billExtraDrops"
  | "ratesIncludeVat"
>;

export const EXTRA_DROP_SIZES: ExtraDropSize[] = [
  "two_tonner",
  "three_tonner",
  "four_tonner",
  "ten_tonner",
  "any",
];

export const TOLLS_OPTIONS: { label: string; value: TollsChoice }[] = [
  { label: "Not stated", value: "unknown" },
  { label: "Billed at cost, on receipts", value: "at_cost" },
  { label: "Included in the rate", value: "included" },
];

export const rateTermsSchema = schema<RateTermsModel>((p) => {
  min(p.includedDrops, 1, { message: "At least one drop." });
  min(p.foulTripPercent, 0, { message: "0 to 100%." });
  max(p.foulTripPercent, 100, { message: "0 to 100%." });
  min(p.backloadPercent, 0, { message: "0 to 100%." });
  max(p.backloadPercent, 100, { message: "0 to 100%." });
  min(p.demurrageFreeHours, 0, { message: "Can't be negative." });
  min(p.demurrageHourlyRate, 0, { message: "Can't be negative." });
  min(p.otherLocationCharge, 0, { message: "Can't be negative." });
  for (const size of EXTRA_DROP_SIZES) {
    min(p.extraDrop[size], 0, { message: "Can't be negative." });
  }
});

export function extraDropSizeLabel(size: ExtraDropSize): string {
  return size === "any" ? "Any size" : tonnageLabel(size);
}

export function emptyTerms(): RateTermsModel {
  return {
    includedDrops: null,
    extraDrop: {
      two_tonner: null,
      three_tonner: null,
      four_tonner: null,
      ten_tonner: null,
      any: null,
    },
    foulTripPercent: null,
    backloadPercent: null,
    demurrageFreeHours: null,
    demurrageHourlyRate: null,
    otherLocationCharge: null,
    tolls: "unknown",
    billExtraDrops: false,
    ratesIncludeVat: false,
  };
}

export function termsFromDto(dto: RateAgreementDto): RateTermsModel {
  const terms = emptyTerms();
  for (const charge of dto.extraDropCharges ?? []) {
    terms.extraDrop[charge.tonnage ?? "any"] = charge.amount ?? null;
  }

  return {
    ...terms,
    includedDrops: dto.includedDrops ?? null,
    foulTripPercent: dto.foulTripPercent ?? null,
    backloadPercent: dto.backloadPercent ?? null,
    demurrageFreeHours: dto.demurrageFreeHours ?? null,
    demurrageHourlyRate: dto.demurrageHourlyRate ?? null,
    otherLocationCharge: dto.otherLocationCharge ?? null,
    tolls:
      dto.tollsAndParkingAtCost == null
        ? "unknown"
        : dto.tollsAndParkingAtCost
          ? "at_cost"
          : "included",
    billExtraDrops: dto.billExtraDrops ?? false,
    ratesIncludeVat: dto.ratesIncludeVat ?? false,
  };
}

export function termsToValue(terms: RateTermsModel): RateTermsValue {
  const extraDropCharges: RateExtraDropChargeDto[] = EXTRA_DROP_SIZES.filter(
    (size) => terms.extraDrop[size] != null,
  ).map((size) => ({
    tonnage: size === "any" ? undefined : size,
    amount: terms.extraDrop[size]!,
  }));

  return {
    includedDrops: terms.includedDrops,
    extraDropCharges,
    foulTripPercent: terms.foulTripPercent,
    backloadPercent: terms.backloadPercent,
    demurrageFreeHours: terms.demurrageFreeHours,
    demurrageHourlyRate: terms.demurrageHourlyRate,
    otherLocationCharge: terms.otherLocationCharge,
    tollsAndParkingAtCost: terms.tolls === "unknown" ? null : terms.tolls === "at_cost",
    billExtraDrops: terms.billExtraDrops,
    ratesIncludeVat: terms.ratesIncludeVat,
  };
}

const peso = (amount: number): string =>
  `₱${amount.toLocaleString("en-PH", { maximumFractionDigits: 2 })}`;

/** The terms as the details page lists them; null means the quotation doesn't say. */
export function describeTerms(dto: RateAgreementDto): { label: string; value: string | null }[] {
  const included = dto.includedDrops ?? 1;
  const charges = [...(dto.extraDropCharges ?? [])]
    .sort(
      (a, b) =>
        EXTRA_DROP_SIZES.indexOf(a.tonnage ?? "any") - EXTRA_DROP_SIZES.indexOf(b.tonnage ?? "any"),
    )
    .map((c) => `${peso(c.amount ?? 0)} (${extraDropSizeLabel(c.tonnage ?? "any")})`)
    .join(", ");

  return [
    {
      label: "Drops in the rate",
      value: dto.includedDrops == null ? null : `${dto.includedDrops}`,
    },
    {
      label: "Each further drop",
      value: charges ? `${charges} after drop ${included}` : null,
    },
    {
      label: "Foul trip",
      value: dto.foulTripPercent == null ? null : `${dto.foulTripPercent}% of the rate`,
    },
    {
      label: "Backload",
      value: dto.backloadPercent == null ? null : `${dto.backloadPercent}% of the rate`,
    },
    {
      label: "Demurrage",
      value:
        dto.demurrageHourlyRate == null
          ? null
          : `${dto.demurrageFreeHours ?? 0} h free, then ${peso(dto.demurrageHourlyRate)}/h`,
    },
    {
      label: "Different delivery location",
      value: dto.otherLocationCharge == null ? null : `+${peso(dto.otherLocationCharge)}`,
    },
    {
      label: "Tolls and parking",
      value:
        dto.tollsAndParkingAtCost == null
          ? null
          : dto.tollsAndParkingAtCost
            ? "Billed at cost, on receipts"
            : "Included in the rate",
    },
  ];
}
