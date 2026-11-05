import type {
  DropReturnReason,
  LoadStatus,
  RecordTripReportCommand,
  TripDto,
  TripLoadDto,
  TripReportStopEntry,
  TripStopDto,
} from "@trackwise/shared/api";
import { toClockText, type ChainEntry, type ResolvedTime } from "../utils/report-times";
import { sameVisitPickups } from "../utils/same-visit";

/** What the office types for one block of the report: a warehouse visit or one drop. */
export interface ReportBlock {
  /** The block's first stop id; a warehouse entry covers its whole visit. */
  key: string;
  kind: "pickup" | "drop";
  arrivalRecorded: boolean;
  handlingStartRecorded: boolean;
  handlingFinishRecorded: boolean;
  departureRecorded: boolean;
  arrival: string;
  tempArrival: number | null;
  handlingStart: string;
  handlingFinish: string;
  departure: string;
  tempDeparture: number | null;
  outcome: LoadStatus | null;
  returnedBoxes: number | null;
  returnedBags: number | null;
  returnReason: DropReturnReason | null;
  note: string;
}

export interface ReportModel {
  /** The pre-load day - the first time in the report falls on it. */
  startDay: Date | null;
  garageOutRecorded: boolean;
  garageInRecorded: boolean;
  garageOut: string;
  garageIn: string;
  blocks: ReportBlock[];
}

/** The read-only side of a block: where it is, what was planned, what's already on record. */
export interface BlockInfo {
  key: string;
  kind: "pickup" | "drop";
  title: string;
  stops: TripStopDto[];
  load: TripLoadDto | null;
  /** The first stop - where recorded times and computed dwell come from. */
  stop: TripStopDto;
}

export type TimeField = "arrival" | "handlingStart" | "handlingFinish" | "departure";
export const TIME_FIELDS: readonly TimeField[] = [
  "arrival",
  "handlingStart",
  "handlingFinish",
  "departure",
];
export const GARAGE_OUT = "garage:out";
export const GARAGE_IN = "garage:in";

export const timeKey = (blockKey: string, field: TimeField): string => `${blockKey}:${field}`;

/** Warehouse visits and drops in trip order, one block each. */
export function buildBlocks(trip: TripDto): BlockInfo[] {
  const stops = [...(trip.stops ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  const loads = new Map((trip.loads ?? []).map((l) => [l.id, l] as const));
  const covered = new Set<string>();
  const blocks: BlockInfo[] = [];
  let warehouses = 0;
  let drops = 0;

  for (const stop of stops) {
    if (covered.has(stop.id!)) {
      continue;
    }

    const group = sameVisitPickups(stop, stops).filter((s) => !covered.has(s.id!));
    group.forEach((s) => covered.add(s.id!));
    const isDrop = stop.type === "drop_off";
    const title = isDrop
      ? `Drop ${++drops}`
      : ++warehouses === 1
        ? "Warehouse"
        : `Warehouse ${warehouses}`;
    blocks.push({
      key: stop.id!,
      kind: isDrop ? "drop" : "pickup",
      title,
      stops: group,
      load: loads.get(stop.loadId!) ?? null,
      stop,
    });
  }

  // A second warehouse visit renames the first so both read as numbered.
  if (warehouses > 1) {
    const first = blocks.find((b) => b.title === "Warehouse");
    if (first) first.title = "Warehouse 1";
  }
  return blocks;
}

/** The form's starting values: blank where nothing's recorded, the recorded value where it is. */
export function buildModel(trip: TripDto, blocks: BlockInfo[]): ReportModel {
  return {
    startDay: startDayOf(trip),
    garageOutRecorded: !!trip.garageDepartedAt,
    garageInRecorded: !!trip.garageArrivedAt,
    garageOut: toClockText(trip.garageDepartedAt),
    garageIn: toClockText(trip.garageArrivedAt),
    blocks: blocks.map(({ key, kind, stop, load }) => ({
      key,
      kind,
      arrivalRecorded: !!stop.arrivedAt,
      handlingStartRecorded: !!stop.handlingStartedAt,
      handlingFinishRecorded: !!stop.handlingFinishedAt,
      departureRecorded: !!stop.departedAt,
      arrival: toClockText(stop.arrivedAt),
      tempArrival: stop.temperatureAtArrival ?? null,
      handlingStart: toClockText(stop.handlingStartedAt),
      handlingFinish: toClockText(stop.handlingFinishedAt),
      departure: toClockText(stop.departedAt),
      tempDeparture: stop.temperatureAtDeparture ?? null,
      outcome: kind === "drop" ? (stop.departedAt ? (load?.status ?? null) : "delivered") : null,
      returnedBoxes: load?.returnedBoxes ?? null,
      returnedBags: load?.returnedBags ?? null,
      returnReason: load?.returnReason ?? null,
      note: load?.outcomeNote ?? "",
    })),
  };
}

/** Every time in the report in the order the trip ran - the chain `resolveChain` dates. */
export function chainEntries(trip: TripDto, blocks: BlockInfo[], model: ReportModel): ChainEntry[] {
  const recorded = (value: string | null | undefined) => (value ? new Date(value) : null);
  const entries: ChainEntry[] = [
    { key: GARAGE_OUT, text: model.garageOut, fixed: recorded(trip.garageDepartedAt) },
  ];

  blocks.forEach((info, i) => {
    const block = model.blocks[i];
    const { stop } = info;
    entries.push(
      { key: timeKey(info.key, "arrival"), text: block.arrival, fixed: recorded(stop.arrivedAt) },
      {
        key: timeKey(info.key, "handlingStart"),
        text: block.handlingStart,
        fixed: recorded(stop.handlingStartedAt),
      },
      {
        key: timeKey(info.key, "handlingFinish"),
        text: block.handlingFinish,
        fixed: recorded(stop.handlingFinishedAt),
      },
      {
        key: timeKey(info.key, "departure"),
        text: block.departure,
        fixed: recorded(stop.departedAt),
      },
    );
  });

  entries.push({ key: GARAGE_IN, text: model.garageIn, fixed: recorded(trip.garageArrivedAt) });
  return entries;
}

/** Only what's new: recorded events are left out, and so are blocks with nothing typed. */
export function buildCommand(
  model: ReportModel,
  resolved: ReadonlyMap<string, ResolvedTime>,
): RecordTripReportCommand {
  const iso = (key: string): string | null => {
    const at = resolved.get(key);
    return at instanceof Date ? at.toISOString() : null;
  };

  const stops: TripReportStopEntry[] = [];
  for (const block of model.blocks) {
    const entry: TripReportStopEntry = { stopId: block.key };
    if (!block.arrivalRecorded && iso(timeKey(block.key, "arrival"))) {
      entry.arrivedAt = iso(timeKey(block.key, "arrival"));
      entry.temperatureAtArrival = block.tempArrival;
    }
    if (!block.departureRecorded && iso(timeKey(block.key, "departure"))) {
      entry.departedAt = iso(timeKey(block.key, "departure"));
      // The crew may have recorded loading or unloading from the app already; that one stays.
      entry.handlingStartedAt = block.handlingStartRecorded
        ? null
        : iso(timeKey(block.key, "handlingStart"));
      entry.handlingFinishedAt = block.handlingFinishRecorded
        ? null
        : iso(timeKey(block.key, "handlingFinish"));
      entry.temperatureAtDeparture = block.tempDeparture;
      if (block.kind === "drop") {
        entry.outcome = block.outcome ?? undefined;
        entry.returnedBoxes = block.returnedBoxes;
        entry.returnedBags = block.returnedBags;
        entry.returnReason = block.returnReason ?? undefined;
        entry.outcomeNote = block.note.trim() || null;
      }
    }
    if (entry.arrivedAt || entry.departedAt) {
      stops.push(entry);
    }
  }

  return {
    garageDepartedAt: model.garageOutRecorded ? null : iso(GARAGE_OUT),
    garageArrivedAt: model.garageInRecorded ? null : iso(GARAGE_IN),
    stops,
  };
}

/** The pre-load day: the earliest requested pickup, else the first thing on record, else today. */
function startDayOf(trip: TripDto): Date {
  const candidates = [
    ...(trip.loads ?? []).map((l) => l.requestedPickupDate),
    trip.garageDepartedAt,
    ...(trip.stops ?? []).map((s) => s.arrivedAt),
    trip.dispatchedAt,
  ]
    .filter((v): v is string => !!v)
    .map((v) => new Date(v).getTime());
  const first = candidates.length ? new Date(Math.min(...candidates)) : new Date();
  return new Date(first.getFullYear(), first.getMonth(), first.getDate());
}
