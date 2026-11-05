import { Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { disabled, form, FormField, FormRoot, required, validate } from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import { DRIVING_ROLES } from "@trackwise/shared";
import {
  Api,
  getCrewPlan,
  type EmployeeDto,
  type HelperDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { Stack, UiButton, UiTextField } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import {
  SearchEmployee,
  SearchHelper,
  SearchTruck,
  UiFormField,
  ValidatedForm,
} from "@/shared/components";
import { dateToIso } from "@/shared/utils";
import { TripWizardStore } from "../../store/trip-wizard-store";

interface TripBasicModel {
  tripName: string;
  // Truck assignment is optional - trip can be created without truck (e.g., from load board).
  truck: TruckDto | null;
  // Crew are paired with a truck per day, so the trip records its own driver. Picking a truck fills
  // the crew from today's plan; left empty it defaults to the truck's main driver.
  driver: EmployeeDto | null;
  // The helper(s) riding this trip, if any - per trip/day, not per stop.
  helper: HelperDto | null;
  secondHelper: HelperDto | null;
}

@Component({
  selector: "app-trip-wizard-basic",
  templateUrl: "./trip-wizard-basic.html",
  imports: [
    FormField,
    FormRoot,
    RouterLink,
    SearchEmployee,
    SearchHelper,
    SearchTruck,
    Stack,
    UiButton,
    UiFormField,
    UiTextField,
    ValidatedForm,
  ],
})
export class TripWizardBasic {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  protected readonly store = inject(TripWizardStore);

  public readonly disabled = input<boolean>(false);

  protected readonly drivingRoles = DRIVING_ROLES;
  protected readonly today = dateToIso(new Date());

  protected readonly model = signal<TripBasicModel>({
    tripName: "",
    truck: null,
    driver: null,
    helper: null,
    secondHelper: null,
  });

  private readonly pickedTruckId = computed(() => this.model().truck?.id ?? null);

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.tripName, { message: "Trip name is required." });
      disabled(p.truck, { when: () => this.disabled() });
      validate(p.secondHelper, ({ valueOf }) => {
        const first = valueOf(p.helper);
        const second = valueOf(p.secondHelper);
        return first?.id && second?.id && first.id === second.id
          ? { kind: "sameHelper", message: "Pick a different second helper." }
          : null;
      });
    },
    {
      submission: {
        action: async () => {
          // A disabled truck is treated as "no truck" in the payload.
          const truck = this.form.truck().disabled() ? null : this.model().truck;

          const { helper, secondHelper } = this.model();
          // Crew rotate across trucks daily - an unpicked driver falls back to the truck's main one.
          const driver = this.model().driver ?? truck?.mainDriver ?? null;

          // Update store with basic info.
          this.store.setBasicInfo({
            tripName: this.model().tripName,
            truckId: truck?.id ?? null,
            truckNumber: truck?.number ?? null,
            truckVehicleCapacity: truck?.vehicleCapacity ?? 0,
            helperId: helper?.id ?? null,
            helperName: helper?.name ?? null,
            driverId: driver?.id ?? null,
            driverName: driver?.fullName ?? null,
            secondHelperId: secondHelper?.id ?? null,
            secondHelperName: secondHelper?.name ?? null,
          });

          // Navigate to next step.
          this.store.nextStep();
          return undefined;
        },
      },
    },
  );

  constructor() {
    // Seed the form from the store (e.g. when editing an existing trip) with id + display name,
    // like the driver. A bare id string has no `.id`, so pressing Next without re-picking the
    // truck or helpers used to drop them from the trip.
    effect(() => {
      const tripName = this.store.tripName();
      const truckId = this.store.truckId();
      const helperId = this.store.helperId();
      const secondHelperId = this.store.secondHelperId();
      const driverId = this.store.driverId();
      const driverName = this.store.driverName();

      if (tripName || truckId || helperId) {
        this.model.update((v) => ({
          ...v,
          tripName,
          truck: truckId
            ? ({
                id: truckId,
                number: this.store.truckNumber(),
                vehicleCapacity: this.store.truckVehicleCapacity(),
              } as TruckDto)
            : null,
          helper: helperId ? ({ id: helperId, name: this.store.helperName() } as HelperDto) : null,
          secondHelper: secondHelperId
            ? ({ id: secondHelperId, name: this.store.secondHelperName() } as HelperDto)
            : null,
          driver: driverId ? ({ id: driverId, fullName: driverName } as EmployeeDto) : null,
        }));
      }
    });

    // A newly picked truck brings its crew from today's plan. The trip's own truck (when editing)
    // doesn't, so reopening a trip never swaps the crew it was saved with.
    effect(() => {
      const truckId = this.pickedTruckId();
      if (truckId && truckId !== untracked(this.store.truckId)) {
        untracked(() => void this.fillCrewFromPlan(truckId));
      }
    });
  }

  /** Fills an empty crew from the truck's row in today's crew plan; a crew already picked stays. */
  private async fillCrewFromPlan(truckId: string): Promise<void> {
    const { driver, helper, secondHelper } = this.model();
    if (driver || helper || secondHelper) {
      return;
    }

    const [row] = (await this.api.invoke(getCrewPlan, { TruckId: truckId })) ?? [];
    if (!row?.assignmentId || this.pickedTruckId() !== truckId) {
      return;
    }

    this.model.update((v) => ({
      ...v,
      driver: row.driverId ? ({ id: row.driverId, fullName: row.driverName } as EmployeeDto) : null,
      helper: row.helperId ? ({ id: row.helperId, name: row.helperName } as HelperDto) : null,
      secondHelper: row.secondHelperId
        ? ({ id: row.secondHelperId, name: row.secondHelperName } as HelperDto)
        : null,
    }));
  }
}
