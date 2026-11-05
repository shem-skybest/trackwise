import {
  Component,
  computed,
  inject,
  input,
  model,
  output,
  signal,
  type OnInit,
} from "@angular/core";
import { FormBuilder } from "@angular/forms";
import { form, FormField, FormRoot, required } from "@angular/forms/signals";
import {
  Stack,
  UiButton,
  UiDialog,
  UiFormField,
  UiSelectField,
  type LocationDto,
  type SelectOption,
  type StopType,
} from "@trackwise/shared";

export interface FormModel {
  stopType: StopType;
  locationId: string;
}

const STOP_TYPE_LABELS: Record<StopType, string> = {
  pickup: "Pickup Location",
  dropoff: "Delivery Destination",
  waypoint: "Waypoint Stop",
  cross_dock_staging: "Crossdocking Staging",
};

const STOP_TYPE_OPTIONS: SelectOption<StopType>[] = (
  Object.keys(STOP_TYPE_LABELS) as StopType[]
).map((type) => ({
  label: STOP_TYPE_LABELS[type],
  value: type,
}));

@Component({
  selector: "add-stop-dilog",
  templateUrl: "add-stop-dialog.html",
  imports: [FormField, UiDialog, UiFormField, FormRoot, UiSelectField, UiButton, Stack],
})
export class RouteStopDialog implements OnInit {
  isOpen = model<boolean>(false);
  locations = input<LocationDto[]>([]);
  closed = output<void>();
  submitted = output<FormModel>();
  private readonly fb = inject(FormBuilder).nonNullable;

  protected readonly stopTypeOptions = STOP_TYPE_OPTIONS;
  protected readonly locationOptions = computed<SelectOption<string>[]>(() =>
    this.locations().map((d) => ({
      label: d.name!,
      value: d.id!,
    })),
  );

  protected readonly stopModel = signal<FormModel>({
    stopType: "pickup",
    locationId: "",
  });

  protected readonly form = form(this.stopModel, (s) => {
    required(s.stopType);
    required(s.locationId);
  });

  constructor() {}

  ngOnInit() {}

  close() {
    this.isOpen.set(false);
    this.closed.emit();
  }

  onSubmit() {
    if (!this.form().valid()) return;

    const val = this.stopModel();

    this.submitted.emit({
      stopType: val.stopType!,
      locationId: val.locationId,
    });

    this.close();
  }
}
