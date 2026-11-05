import {
  afterNextRender,
  Component,
  ElementRef,
  inject,
  input,
  linkedSignal,
  output,
} from "@angular/core";
import { form, FormField, FormRoot, required, validate } from "@angular/forms/signals";
import { DRIVING_ROLES } from "@trackwise/shared";
import {
  Api,
  updateTrip,
  type EmployeeDto,
  type HelperDto,
  type TripDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { ToastService } from "@trackwise/shared/services";
import { Grid, Stack, Typography, UiButton, ValidatedForm } from "@trackwise/shared/ui";
import { SearchEmployee, SearchHelper, SearchTruck, UiFormField } from "@/shared/components";
import { headerUpdate } from "../../utils/trip-header";

interface HeaderModel {
  truck: TruckDto | null;
  driver: EmployeeDto | null;
  helper: HelperDto | null;
  secondHelper: HelperDto | null;
}

/**
 * Changes the trip's truck and crew from the crew report, through the same `UpdateTrip` as Edit Trip
 * so the same rules apply (who's available that day, no one on two trips). Opens on the truck; Enter
 * moves on, Ctrl+S saves, Esc closes.
 */
@Component({
  selector: "app-report-header-editor",
  templateUrl: "./report-header-editor.html",
  host: { "(keydown.escape)": "closed.emit()" },
  imports: [
    FormField,
    FormRoot,
    Grid,
    SearchEmployee,
    SearchHelper,
    SearchTruck,
    Stack,
    Typography,
    UiButton,
    UiFormField,
    ValidatedForm,
  ],
})
export class ReportHeaderEditor {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly trip = input.required<TripDto>();
  /** The day crew availability is checked on, as the API checks it. */
  public readonly crewDay = input.required<string>();
  public readonly saved = output<void>();
  public readonly closed = output<void>();

  protected readonly drivingRoles = DRIVING_ROLES;

  protected readonly model = linkedSignal<HeaderModel>(() => {
    const trip = this.trip();
    return {
      truck: trip.truckId ? ({ id: trip.truckId, number: trip.truckNumber } as TruckDto) : null,
      driver: trip.driverId
        ? ({ id: trip.driverId, fullName: trip.driverName } as EmployeeDto)
        : null,
      helper: trip.helperId ? ({ id: trip.helperId, name: trip.helperName } as HelperDto) : null,
      secondHelper: trip.secondHelperId
        ? ({ id: trip.secondHelperId, name: trip.secondHelperName } as HelperDto)
        : null,
    };
  });

  protected readonly form = form(
    this.model,
    (p) => {
      required(p.truck, {
        message: "Pick the truck.",
        when: () => !!this.trip().truckId,
      });
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
          const { truck, driver, helper, secondHelper } = this.model();
          const trip = this.trip();
          try {
            await this.api.invoke(updateTrip, {
              id: trip.id!,
              body: headerUpdate(trip, {
                truckId: truck?.id ?? null,
                driverId: driver?.id ?? null,
                helperId: helper?.id ?? null,
                secondHelperId: secondHelper?.id ?? null,
              }),
            });
            this.toast.showSuccess("Truck and crew updated");
            this.saved.emit();
          } catch {
            // The API error interceptor has already shown why (e.g. a crew member on leave that day).
          }
          return undefined;
        },
      },
    },
  );

  constructor() {
    const host = inject<ElementRef<HTMLElement>>(ElementRef);
    afterNextRender(() => host.nativeElement.querySelector<HTMLInputElement>("input")?.focus());
  }
}
