import { maxLength, min, required, validate, type SchemaPathTree } from "@angular/forms/signals";
import type {
  AccidentType,
  CreateIncidentCommand,
  CustomerDto,
  EmployeeDto,
  HelperDto,
  IncidentDto,
  IncidentKind,
  IncidentSeverity,
  TripDto,
  TruckDto,
} from "@trackwise/shared/api";
import { dateToIso, isoToDate } from "@/shared/utils";

export interface IncidentModel {
  kind: IncidentKind;
  severity: IncidentSeverity;
  accidentType: AccidentType | null;
  occurredAt: Date | null;
  summary: string;
  prevention: string;
  place: string;
  tripId: string | null;
  tripNumber: number | null;
  customer: CustomerDto | null;
  truck: TruckDto | null;
  driver: EmployeeDto | null;
  helper: HelperDto | null;
  secondHelper: HelperDto | null;
  anyInjuries: boolean;
  numberOfInjuries: number | null;
  injuryDescription: string;
  vehicleDamaged: boolean;
  vehicleDamageDescription: string;
  vehicleDrivable: boolean;
  estimatedDamageCost: number | null;
  policeReportFiled: boolean;
  policeReportNumber: string;
  policeStation: string;
  barangayBlotterNumber: string;
  insuranceClaimNumber: string;
  insuranceNotifiedOn: Date | null;
  thirdPartyCompensation: number | null;
  crewChargeAmount: number | null;
  crewChargeNote: string;
}

export function emptyIncident(): IncidentModel {
  return {
    kind: "accident",
    severity: "minor",
    accidentType: null,
    occurredAt: new Date(),
    summary: "",
    prevention: "",
    place: "",
    tripId: null,
    tripNumber: null,
    customer: null,
    truck: null,
    driver: null,
    helper: null,
    secondHelper: null,
    anyInjuries: false,
    numberOfInjuries: null,
    injuryDescription: "",
    vehicleDamaged: false,
    vehicleDamageDescription: "",
    vehicleDrivable: true,
    estimatedDamageCost: null,
    policeReportFiled: false,
    policeReportNumber: "",
    policeStation: "",
    barangayBlotterNumber: "",
    insuranceClaimNumber: "",
    insuranceNotifiedOn: null,
    thirdPartyCompensation: null,
    crewChargeAmount: null,
    crewChargeNote: "",
  };
}

/**
 * A report started from a trip: its truck, crew and client (the client only when it is one), timed
 * at the trip's last recorded event - the office usually files it after the crew's message.
 */
export function incidentFromTrip(trip: TripDto): IncidentModel {
  const recorded = (trip.stops ?? [])
    .flatMap((s) => [s.arrivedAt, s.departedAt])
    .concat(trip.garageDepartedAt)
    .filter((t): t is string => !!t)
    .map((t) => new Date(t).getTime());
  const customers = [
    ...new Map((trip.loads ?? []).map((l) => [l.customer?.id, l.customer])).values(),
  ];
  return {
    ...emptyIncident(),
    occurredAt: recorded.length ? new Date(Math.max(...recorded)) : new Date(),
    tripId: trip.id ?? null,
    tripNumber: trip.number ?? null,
    customer: customers.length === 1 && customers[0] ? (customers[0] as CustomerDto) : null,
    truck: trip.truckId ? ({ id: trip.truckId, number: trip.truckNumber } as TruckDto) : null,
    driver: trip.driverId
      ? ({ id: trip.driverId, fullName: trip.driverName } as EmployeeDto)
      : null,
    helper: helperRef(trip.helperId, trip.helperName),
    secondHelper: helperRef(trip.secondHelperId, trip.secondHelperName),
  };
}

export function incidentFromDto(dto: IncidentDto): IncidentModel {
  return {
    kind: dto.kind ?? "other",
    severity: dto.severity ?? "minor",
    accidentType: dto.accidentType ?? null,
    occurredAt: dto.occurredAt ? new Date(dto.occurredAt) : null,
    summary: dto.summary ?? "",
    prevention: dto.prevention ?? "",
    place: dto.place ?? "",
    tripId: dto.tripId ?? null,
    tripNumber: dto.tripNumber ?? null,
    customer: dto.customerId
      ? ({ id: dto.customerId, name: dto.customerName } as CustomerDto)
      : null,
    truck: dto.truckId ? ({ id: dto.truckId, number: dto.truckNumber } as TruckDto) : null,
    driver: dto.driverId ? ({ id: dto.driverId, fullName: dto.driverName } as EmployeeDto) : null,
    helper: helperRef(dto.helperId, dto.helperName),
    secondHelper: helperRef(dto.secondHelperId, dto.secondHelperName),
    anyInjuries: dto.anyInjuries ?? false,
    numberOfInjuries: dto.numberOfInjuries ?? null,
    injuryDescription: dto.injuryDescription ?? "",
    vehicleDamaged: dto.vehicleDamaged ?? false,
    vehicleDamageDescription: dto.vehicleDamageDescription ?? "",
    vehicleDrivable: dto.vehicleDrivable ?? true,
    estimatedDamageCost: dto.estimatedDamageCost ?? null,
    policeReportFiled: dto.policeReportFiled ?? false,
    policeReportNumber: dto.policeReportNumber ?? "",
    policeStation: dto.policeStation ?? "",
    barangayBlotterNumber: dto.barangayBlotterNumber ?? "",
    insuranceClaimNumber: dto.insuranceClaimNumber ?? "",
    insuranceNotifiedOn: isoToDate(dto.insuranceNotifiedOn),
    thirdPartyCompensation: dto.thirdPartyCompensation ?? null,
    crewChargeAmount: dto.crewChargeAmount ?? null,
    crewChargeNote: dto.crewChargeNote ?? "",
  };
}

/** The whole report as the API takes it - the same body for create and update. */
export function incidentCommand(v: IncidentModel): CreateIncidentCommand {
  const text = (value: string) => value.trim() || null;
  return {
    kind: v.kind,
    severity: v.severity,
    accidentType: v.kind === "accident" ? (v.accidentType ?? undefined) : undefined,
    occurredAt: v.occurredAt!.toISOString(),
    summary: v.summary.trim(),
    prevention: text(v.prevention),
    place: text(v.place),
    tripId: v.tripId,
    customerId: v.customer?.id ?? null,
    truckId: v.truck?.id ?? null,
    driverId: v.driver?.id ?? null,
    helperId: v.helper?.id ?? null,
    secondHelperId: v.secondHelper?.id ?? null,
    anyInjuries: v.anyInjuries,
    numberOfInjuries: v.anyInjuries ? v.numberOfInjuries : null,
    injuryDescription: v.anyInjuries ? text(v.injuryDescription) : null,
    vehicleDamaged: v.vehicleDamaged,
    vehicleDamageDescription: v.vehicleDamaged ? text(v.vehicleDamageDescription) : null,
    vehicleDrivable: v.vehicleDrivable,
    estimatedDamageCost: v.estimatedDamageCost,
    policeReportFiled: v.policeReportFiled,
    policeReportNumber: v.policeReportFiled ? text(v.policeReportNumber) : null,
    policeStation: v.policeReportFiled ? text(v.policeStation) : null,
    barangayBlotterNumber: text(v.barangayBlotterNumber),
    insuranceClaimNumber: text(v.insuranceClaimNumber),
    insuranceNotifiedOn: dateToIso(v.insuranceNotifiedOn),
    thirdPartyCompensation: v.thirdPartyCompensation,
    crewChargeAmount: v.crewChargeAmount,
    crewChargeNote: text(v.crewChargeNote),
  };
}

/** The rules the add and edit pages share - the API checks the same ones. */
export function incidentSchema(p: SchemaPathTree<IncidentModel>): void {
  required(p.occurredAt, { message: "Enter when it happened." });
  validate(p.occurredAt, ({ value }) =>
    value() && value()!.getTime() > Date.now() + 5 * 60 * 1000
      ? { kind: "future", message: "That's in the future." }
      : null,
  );
  required(p.summary, { message: "Say what happened." });
  maxLength(p.summary, 4000, { message: "Keep it under 4,000 characters." });
  maxLength(p.prevention, 4000, { message: "Keep it under 4,000 characters." });
  maxLength(p.place, 500, { message: "Keep the place under 500 characters." });
  validate(p.numberOfInjuries, ({ value, valueOf }) =>
    valueOf(p.anyInjuries) && !(value() && value()! > 0)
      ? { kind: "required", message: "Enter how many people were hurt." }
      : null,
  );
  min(p.estimatedDamageCost, 0, { message: "Can't be negative." });
  min(p.thirdPartyCompensation, 0, { message: "Can't be negative." });
  min(p.crewChargeAmount, 0, { message: "Can't be negative." });
  maxLength(p.crewChargeNote, 500, { message: "Keep it under 500 characters." });
  validate(p.secondHelper, ({ value, valueOf }) =>
    value()?.id && value()!.id === valueOf(p.helper)?.id
      ? { kind: "duplicate", message: "The same helper is listed twice." }
      : null,
  );
}

function helperRef(id?: string | null, name?: string | null): HelperDto | null {
  return id ? ({ id, name: name ?? "" } as HelperDto) : null;
}
