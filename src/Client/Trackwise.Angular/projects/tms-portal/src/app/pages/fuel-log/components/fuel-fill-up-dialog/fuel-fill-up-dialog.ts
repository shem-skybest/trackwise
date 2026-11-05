import { DatePipe, DecimalPipe } from "@angular/common";
import { Component, computed, effect, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, max, maxLength, min, required } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  Api,
  getTrips,
  getTrucks,
  recordFuelFillUp,
  updateFuelFillUp,
  type FleetCard,
  type FuelFillUpDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { fleetCardOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { ExpenseReceiptUpload } from "@/shared/components/documents";

interface FillUpModel {
  filledAt: Date | null;
  truckId: string | null;
  fleetCard: FleetCard | null;
  liters: number | null;
  amount: number | null;
  odometerReading: number | null;
  tripId: string | null;
  station: string;
  notes: string;
  receiptBlobPath: string | null;
}

interface TripOption {
  id: string;
  label: string;
}

/**
 * Logs or corrects one fill-up, typed from its receipt. The price per liter shows as it's worked
 * out, so liters and amount typed into each other's box stand out before saving.
 */
@Component({
  selector: "app-fuel-fill-up-dialog",
  templateUrl: "./fuel-fill-up-dialog.html",
  providers: [DatePipe],
  imports: [
    DecimalPipe,
    ExpenseReceiptUpload,
    FormField,
    FormRoot,
    Grid,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextField,
    ValidatedForm,
  ],
})
export class FuelFillUpDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  private readonly datePipe = inject(DatePipe);

  /** The fill-up to correct; a new one when null. */
  public readonly fillUp = input<FuelFillUpDto | null>(null);
  public readonly open = model(false);
  public readonly saved = output<FuelFillUpDto>();

  protected readonly cardOptions = fleetCardOptions;
  protected readonly trucks = signal<TruckDto[]>([]);
  protected readonly trips = signal<TripOption[]>([]);
  protected readonly loadingTrips = signal(false);
  /** Bumped to give each fill-up a fresh receipt picker. */
  protected readonly receiptSlot = signal(0);
  protected readonly isEdit = computed(() => this.fillUp() !== null);
  protected readonly model = signal<FillUpModel>(emptyModel());
  private readonly truckId = computed(() => this.model().truckId);

  protected readonly pricePerLiter = computed(() => {
    const { liters, amount } = this.model();
    return liters && amount ? amount / liters : null;
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.filledAt, { message: "Enter when the tank was filled." });
      required(p.truckId, { message: "Pick the truck." });
      required(p.liters, { message: "Enter the liters." });
      min(p.liters, 0.01, { message: "Liters must be above zero." });
      max(p.liters, 1000, { message: "A tank takes at most 1,000 liters." });
      required(p.amount, { message: "Enter the amount paid." });
      min(p.amount, 0.01, { message: "The amount must be above zero." });
      min(p.odometerReading, 0, { message: "The odometer can't be negative." });
      maxLength(p.station, 255, { message: "Keep it under 255 characters." });
      maxLength(p.notes, 2000, { message: "Keep it under 2,000 characters." });
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            filledAt: v.filledAt!.toISOString(),
            truckId: v.truckId!,
            fleetCard: v.fleetCard ?? undefined,
            liters: v.liters!,
            amount: v.amount!,
            odometerReading: v.odometerReading,
            tripId: v.tripId,
            station: v.station.trim() || null,
            notes: v.notes.trim() || null,
            receiptBlobPath: v.receiptBlobPath,
          };

          try {
            const existing = this.fillUp();
            const saved = existing
              ? await this.api.invoke(updateFuelFillUp, { id: existing.id!, body })
              : await this.api.invoke(recordFuelFillUp, { body });
            this.toast.showSuccess(existing ? "Fill-up updated." : "Fill-up logged.");
            this.saved.emit(saved);
            if (existing) {
              this.open.set(false);
            } else {
              this.startNext(v);
            }
          } catch (error) {
            this.toast.showError(getApiErrorMessage(error, "Could not save the fill-up."));
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    effect(() => void this.loadTrips(this.truckId()));
  }

  protected async onOpened(): Promise<void> {
    const fill = this.fillUp();
    this.model.set(
      fill
        ? {
            filledAt: fill.filledAt ? new Date(fill.filledAt) : new Date(),
            truckId: fill.truckId ?? null,
            fleetCard: fill.fleetCard ?? null,
            liters: fill.liters ?? null,
            amount: fill.amount ?? null,
            odometerReading: fill.odometerReading ?? null,
            tripId: fill.tripId ?? null,
            station: fill.station ?? "",
            notes: fill.notes ?? "",
            receiptBlobPath: null,
          }
        : emptyModel(),
    );
    this.receiptSlot.update((n) => n + 1);

    if (this.trucks().length === 0) {
      const result = await this.api.invoke(getTrucks, { PageSize: 200, OrderBy: "Number" });
      this.trucks.set(result?.items ?? []);
    }
  }

  protected onReceiptUploaded(path: string): void {
    this.model.update((v) => ({ ...v, receiptBlobPath: path }));
  }

  /** Keeps the dialog open for the next receipt: same day and card, another truck. */
  private startNext(previous: FillUpModel): void {
    this.model.set({ ...emptyModel(), filledAt: previous.filledAt, fleetCard: previous.fleetCard });
    this.receiptSlot.update((n) => n + 1);
    this.form().reset();
  }

  private async loadTrips(truckId: string | null): Promise<void> {
    if (!truckId) {
      this.trips.set([]);
      return;
    }

    this.loadingTrips.set(true);
    try {
      const result = await this.api.invoke(getTrips, {
        TruckId: truckId,
        OrderBy: "-CreatedAt",
        PageSize: 30,
      });
      this.trips.set(
        (result?.items ?? []).map((trip) => ({
          id: trip.id!,
          label: `#${trip.number} · ${this.datePipe.transform(trip.createdAt, "MMM d")} · ${trip.name}`,
        })),
      );
    } finally {
      this.loadingTrips.set(false);
    }
  }
}

function emptyModel(): FillUpModel {
  return {
    filledAt: new Date(),
    truckId: null,
    fleetCard: "shell",
    liters: null,
    amount: null,
    odometerReading: null,
    tripId: null,
    station: "",
    notes: "",
    receiptBlobPath: null,
  };
}
