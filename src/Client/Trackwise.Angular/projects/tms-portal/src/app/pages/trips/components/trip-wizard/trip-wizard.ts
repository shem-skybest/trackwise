import { Component, effect, inject, input, output } from "@angular/core";
import type { CreateTripLoadCommand, TripLoadDto, TripStopDto } from "@trackwise/shared/api";
import { Alert, Stack, UiStepperImports } from "@trackwise/shared/ui";
import { TripWizardStore } from "../../store/trip-wizard-store";
import { TripWizardBasic } from "../trip-wizard-basic/trip-wizard-basic";
import { TripFormStepLoads } from "../trip-wizard-loads/trip-wizard-loads";
import { TripWizardReview } from "../trip-wizard-review/trip-wizard-review";

export interface TripWizardValue {
  tripName: string;
  truckId?: string | null; // optional - trip can be created without truck assignment
  truckNumber?: string | null; // Display-friendly truck number
  truckVehicleCapacity?: number;
  helperId?: string | null; // optional - the helper riding this trip, if any
  helperName?: string | null; // Display-friendly helper name
  driverId?: string | null; // who drives this trip - defaults to the truck's main driver
  driverName?: string | null;
  secondHelperId?: string | null; // a second helper, for trucks that run with two
  secondHelperName?: string | null;
  newLoads?: CreateTripLoadCommand[] | null;
  attachedLoadIds?: string[] | null; // IDs of existing loads to attach to this trip
  detachedLoads?: TripLoadDto[] | null;
  stops: TripStopDto[];
  totalDistance: number;
  totalCost: number;
  totalLoads: number;
  initialLoads?: TripLoadDto[];
  initialStops?: TripStopDto[];
}

@Component({
  selector: "app-trip-wizard",
  templateUrl: "./trip-wizard.html",
  providers: [TripWizardStore],
  imports: [Alert, Stack, UiStepperImports, TripFormStepLoads, TripWizardBasic, TripWizardReview],
})
export class TripWizard {
  protected readonly store = inject(TripWizardStore);

  public readonly mode = input.required<"create" | "edit">();
  public readonly disabled = input<boolean>(false);
  public readonly initialData = input<Partial<TripWizardValue> | null>(null);

  public readonly save = output<TripWizardValue>();

  // Expose store state for template
  protected readonly activeStep = this.store.activeStep;

  constructor() {
    // Initialize store when initialData changes
    effect(() => {
      const mode = this.mode();
      const initialData = this.initialData();

      if (initialData) {
        this.store.initialize({
          mode,
          tripName: initialData.tripName,
          truckId: initialData.truckId,
          truckNumber: initialData.truckNumber,
          truckVehicleCapacity: initialData.truckVehicleCapacity,
          helperId: initialData.helperId,
          helperName: initialData.helperName,
          driverId: initialData.driverId,
          driverName: initialData.driverName,
          secondHelperId: initialData.secondHelperId,
          secondHelperName: initialData.secondHelperName,
          loads: initialData.initialLoads,
          stops: initialData.initialStops,
          totalDistance: initialData.totalDistance,
          totalCost: initialData.totalCost,
        });
      } else {
        this.store.initialize({ mode });
      }
    });
  }

  protected processStep3(): void {
    this.save.emit(this.store.wizardValue());
  }
}
