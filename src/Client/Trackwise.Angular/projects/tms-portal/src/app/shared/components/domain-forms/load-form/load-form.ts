import {
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  type OnInit,
} from "@angular/core";
import {
  disabled,
  form,
  FormField,
  FormRoot,
  maxLength,
  min,
  pattern,
  required,
} from "@angular/forms/signals";
import { RouterLink } from "@angular/router";
import {
  type Address,
  type ContainerDto,
  type CustomerDto,
  type GeoPoint,
  type HazmatClass,
  type LoadSource,
  type LoadStatus,
  type LoadType,
  type LocationDto,
  type TerminalDto,
  type TruckDto,
} from "@trackwise/shared/api";
import {
  hazmatClassOptions,
  loadSourceOptions,
  loadStatusOptions,
  loadTypeOptions,
} from "@trackwise/shared/api/enums";
import {
  Alert,
  Grid,
  Icon,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiCheckboxField,
  UiCollapsible,
  UiDateField,
  UiFormField,
  UiNumberField,
  UiSelectField,
  UiTextareaField,
  UiTextField,
  ValidatedForm,
} from "@trackwise/shared/ui";
import { AuthService } from "@/core/auth";
import { ToastService } from "@/core/services";
import {
  AddressPickerDialog,
  DirectionMap,
  type RouteChangeEvent,
  type Waypoint,
} from "@/shared/components/maps";
import {
  SearchContainer,
  SearchCustomer,
  SearchLocation,
  SearchTerminal,
  SearchTruck,
} from "@/shared/components/search";
import { atTimeOfDay, Converters } from "@/shared/utils";
import { SaveSiteDialog } from "../save-site-dialog/save-site-dialog";

/**
 * Form value interface for the Load Form.
 */
export interface LoadFormValue {
  name: string;
  type: LoadType;
  source: LoadSource;
  customer: CustomerDto;
  originAddress: Address;
  originLocation: GeoPoint;
  destinationAddress: Address;
  destinationLocation: GeoPoint;
  deliveryCost: number;
  distance: number; // miles, read-only for users
  status?: LoadStatus | null; // only present in edit-mode
  assignedTruckId?: string | null; // optional - load can be created without truck assignment
  assignedDispatcherId: string;
  assignedDispatcherName: string;
  tripId?: string | null;
  tripNumber?: number | null;
  // Schedule
  requestedPickupDate?: string | null;
  requestedDeliveryDate?: string | null;
  // Intermodal IDs (emitted from submit)
  containerId?: string | null;
  originTerminalId?: string | null;
  destinationTerminalId?: string | null;
  // Saved sites - ids only; the pickers resolve them
  originSiteId?: string | null;
  destinationSiteId?: string | null;
  // Intermodal DTOs (for patching from edit-mode initial data)
  container?: ContainerDto | null;
  originTerminal?: TerminalDto | null;
  destinationTerminal?: TerminalDto | null;
  // Notes
  notes?: string | null;
  // Cargo
  commodity?: string | null;
  plannedBoxes?: number | null;
  plannedBags?: number | null;
  // Hazmat / ADR
  isHazmat?: boolean;
  hazmatClass?: HazmatClass | null;
  unNumber?: string | null;
}

const DUMMY_LOCATION: GeoPoint = { longitude: 0, latitude: 0 };

type Side = "origin" | "destination";

/**
 * The signal-form model. Field types mirror the `ui-*-field` wrappers they bind to (e.g.
 * `ui-select-field` is a `FormValueControl<T | null>`), so nullability here is a binding
 * requirement, not a validation one - `required()` in the schema enforces presence.
 */
interface LoadFormModel {
  name: string;
  type: LoadType | null;
  source: LoadSource | null;
  customer: CustomerDto | null;
  originAddress: Address | null;
  originStreetAddress: string;
  originSubdivision: string;
  originLocation: GeoPoint;
  destinationAddress: Address | null;
  destinationStreetAddress: string;
  destinationSubdivision: string;
  destinationLocation: GeoPoint;
  deliveryCost: number | null;
  distance: number | null;
  requestedPickupDate: Date | null;
  requestedDeliveryDate: Date | null;
  container: ContainerDto | null;
  originTerminal: TerminalDto | null;
  destinationTerminal: TerminalDto | null;
  originSite: LocationDto | null;
  destinationSite: LocationDto | null;
  notes: string;
  commodity: string;
  plannedBoxes: number | null;
  plannedBags: number | null;
  isHazmat: boolean;
  hazmatClass: HazmatClass | null;
  unNumber: string;
  status: LoadStatus | null;
  assignedTruck: TruckDto | null;
  assignedDispatcherId: string;
  assignedDispatcherName: string;
  tripId: string | null;
  tripNumber: number | null;
}

const EMPTY: LoadFormModel = {
  name: "",
  type: "general_freight",
  source: "manual",
  customer: null,
  originAddress: null,
  originStreetAddress: "",
  originSubdivision: "",
  originLocation: DUMMY_LOCATION,
  destinationAddress: null,
  destinationStreetAddress: "",
  destinationSubdivision: "",
  destinationLocation: DUMMY_LOCATION,
  deliveryCost: 0,
  distance: 0,
  requestedPickupDate: null,
  requestedDeliveryDate: null,
  container: null,
  originTerminal: null,
  destinationTerminal: null,
  originSite: null,
  destinationSite: null,
  notes: "",
  commodity: "",
  plannedBoxes: null,
  plannedBags: null,
  isHazmat: false,
  hazmatClass: null,
  unNumber: "",
  status: null,
  assignedTruck: null,
  assignedDispatcherId: "",
  assignedDispatcherName: "",
  tripId: null,
  tripNumber: null,
};

@Component({
  selector: "app-load-form",
  templateUrl: "./load-form.html",
  imports: [
    AddressPickerDialog,
    Alert,
    DirectionMap,
    FormField,
    FormRoot,
    Grid,
    Icon,
    RouterLink,
    SearchContainer,
    SaveSiteDialog,
    SearchCustomer,
    SearchLocation,
    SearchTerminal,
    SearchTruck,
    Spinner,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiCheckboxField,
    UiCollapsible,
    UiDateField,
    UiFormField,
    UiNumberField,
    UiSelectField,
    UiTextareaField,
    UiTextField,
    ValidatedForm,
  ],
})
export class LoadForm implements OnInit {
  protected readonly loadTypes = loadTypeOptions;
  protected readonly loadStatuses = loadStatusOptions;
  protected readonly loadSources = loadSourceOptions;
  protected readonly hazmatClasses = hazmatClassOptions;

  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  /** Dialog open states — two-way bound from the parent template. */
  public readonly originPickerOpen = signal(false);
  public readonly destinationPickerOpen = signal(false);

  /** Which end of the route the "save as a delivery location" dialog is saving, if open. */
  protected readonly saveSiteFor = signal<Side | null>(null);

  public readonly mode = input.required<"create" | "edit">();
  public readonly canChangeAssignedTruck = input<boolean>(true);
  public readonly initial = input<Partial<LoadFormValue> | null>(null);
  public readonly isLoading = input(false);
  public readonly mapHeight = input<string>("600px");

  public readonly save = output<LoadFormValue>();
  public readonly remove = output<void>();

  protected readonly model = signal<LoadFormModel>({ ...EMPTY });

  /**
   * `distance`, `assignedDispatcherName`, `tripId` and `tripNumber` are permanently disabled - they
   * are computed or informational. `model()` still holds them (a disabled field drops out of
   * validation, not out of the value), so they ride along to the parent's save.
   */
  protected readonly form = form(
    this.model,
    (p) => {
      required(p.name, { message: "Load name is required." });
      required(p.type, { message: "Load type is required." });
      required(p.source, { message: "Load source is required." });
      required(p.customer, { message: "Customer is required." });
      required(p.originAddress, { message: "Origin address is required." });
      required(p.originStreetAddress, {
        message: "Origin street address is required for address validation.",
      });
      required(p.destinationAddress, { message: "Destination address is required." });
      required(p.destinationStreetAddress, {
        message: "Destination street address is required for address validation.",
      });
      required(p.deliveryCost, { message: "Delivery cost is required." });
      required(p.assignedDispatcherId, { message: "An assigned dispatcher is required." });

      maxLength(p.notes, 2000, { message: "Notes cannot exceed 2000 characters." });
      maxLength(p.unNumber, 16, { message: "UN number cannot exceed 16 characters." });
      maxLength(p.commodity, 100, { message: "Commodity cannot exceed 100 characters." });
      min(p.plannedBoxes, 0, { message: "Boxes can't be negative." });
      min(p.plannedBags, 0, { message: "Bags can't be negative." });
      pattern(p.unNumber, /^UN\d{4}$/i, {
        when: ({ valueOf }) => valueOf(p.unNumber).length > 0,
        message: "Enter a UN number such as UN1203.",
      });

      disabled(p.distance, { when: () => true });
      disabled(p.assignedDispatcherName, { when: () => true });
      disabled(p.tripId, { when: () => true });
      disabled(p.tripNumber, { when: () => true });
      // Sites are filtered to the load's customer, so there is nothing to offer before one is set.
      disabled(p.originSite, { when: ({ valueOf }) => !valueOf(p.customer) });
      disabled(p.destinationSite, { when: ({ valueOf }) => !valueOf(p.customer) });
      disabled(p.assignedTruck, { when: () => !this.canChangeAssignedTruck() });
    },
    {
      submission: {
        action: async () => {
          this.save.emit(this.toFormValue());
          return undefined;
        },
      },
    },
  );

  /** `tripNumber` is a disabled, informational number rendered by a string-valued text field. */
  protected readonly tripNumberText = computed(() => this.model().tripNumber?.toString() ?? "");

  /** An edit-mode load carries only the truck's ID; `app-search-truck` resolves it to the DTO. */
  protected readonly initialTruckId = computed(() => this.initial()?.assignedTruckId ?? null);

  protected readonly initialOriginSiteId = computed(() => this.initial()?.originSiteId ?? null);
  protected readonly initialDestinationSiteId = computed(
    () => this.initial()?.destinationSiteId ?? null,
  );
  protected readonly customerId = computed(() => this.model().customer?.id ?? null);

  /**
   * The site whose address is already in the form, per side. A picker that merely resolves the
   * load's existing site id must not overwrite the address the load was saved with - only a new
   * pick copies the site's address across.
   */
  private readonly appliedSiteIds: Record<Side, string | null> = {
    origin: null,
    destination: null,
  };

  /** Map waypoint built from the form's `originLocation` (skipped if it is the dummy (0,0)). */
  protected readonly originWaypoint = computed(() => {
    const loc = this.model().originLocation;
    if (loc.longitude === 0 && loc.latitude === 0) {
      return null;
    }
    return { id: "origin", location: loc };
  });

  /** Map waypoint built from the form's `destinationLocation` (skipped if it is the dummy (0,0)). */
  protected readonly destinationWaypoint = computed(() => {
    const loc = this.model().destinationLocation;
    if (loc.longitude === 0 && loc.latitude === 0) {
      return null;
    }
    return { id: "destination", location: loc };
  });

  /** Filtered waypoints for the direction map — drops null entries. */
  protected readonly mapWaypoints = computed(() => {
    const pts = [this.originWaypoint(), this.destinationWaypoint()].filter(
      (w): w is Waypoint => w !== null,
    );
    return pts;
  });

  constructor() {
    effect(() => this.applySite("origin", this.model().originSite));
    effect(() => this.applySite("destination", this.model().destinationSite));

    // The server refuses another customer's outlet, so drop one as soon as the customer changes.
    effect(() => {
      const customerId = this.customerId();
      const { originSite, destinationSite } = untracked(this.model);
      const foreign = (site: LocationDto | null) =>
        !!site?.customerId && site.customerId !== customerId;

      if (foreign(originSite) || foreign(destinationSite)) {
        this.model.update((v) => ({
          ...v,
          originSite: foreign(v.originSite) ? null : v.originSite,
          destinationSite: foreign(v.destinationSite) ? null : v.destinationSite,
        }));
      }
    });

    effect(() => {
      const initialData = this.initial();
      if (!initialData) {
        return;
      }

      // Prevent overwriting user changes if the form is dirty in edit-mode
      if (this.mode() === "edit" && this.form().dirty()) {
        return;
      }

      this.patch(initialData);
    });
  }

  ngOnInit(): void {
    if (this.mode() === "create") {
      this.setCurrentDispatcher();
    }
  }

  /** Open the origin address picker dialog. */
  protected openOriginPicker(): void {
    this.originPickerOpen.set(true);
  }

  /** Open the destination address picker dialog. */
  protected openDestinationPicker(): void {
    this.destinationPickerOpen.set(true);
  }

  /**
   * Called when the origin address picker dialog confirms a selection.
   * The dialog emits a fully-formed Address (including street/subdivision/zipcode from the text inputs)
   * and a GeoPoint for the pinned location. We fold these into the form model.
   */
  protected onOriginSelected(selection: { address: Address; location: GeoPoint }): void {
    this.model.update((v) => ({
      ...v,
      originAddress: selection.address,
      originLocation: selection.location,
      originStreetAddress: selection.address.streetAddress ?? "",
      originSubdivision: selection.address.subdivision ?? "",
      // A hand-picked address is no longer the saved site's.
      originSite: null,
    }));
    this.appliedSiteIds.origin = null;
    this.originPickerOpen.set(false);
  }

  /**
   * Called when the destination address picker dialog confirms a selection.
   */
  protected onDestinationSelected(selection: { address: Address; location: GeoPoint }): void {
    this.model.update((v) => ({
      ...v,
      destinationAddress: selection.address,
      destinationLocation: selection.location,
      destinationStreetAddress: selection.address.streetAddress ?? "",
      destinationSubdivision: selection.address.subdivision ?? "",
      destinationSite: null,
    }));
    this.appliedSiteIds.destination = null;
    this.destinationPickerOpen.set(false);
  }

  protected openSaveSite(side: Side): void {
    this.saveSiteFor.set(side);
  }

  /** The dialog saved the typed address as a site; link it without re-copying its address. */
  protected onSiteSaved(site: LocationDto): void {
    const side = this.saveSiteFor();
    if (!side) {
      return;
    }
    this.appliedSiteIds[side] = site.id ?? null;
    this.model.update((v) =>
      side === "origin" ? { ...v, originSite: site } : { ...v, destinationSite: site },
    );
  }

  /**
   * Copies a newly picked site into that side's address and pin, and for the drop its default
   * cut-off into deliver-by. A city-level outlet with no street keeps its name as the street, so
   * the load's address still says where it went.
   */
  private applySite(side: Side, site: LocationDto | null): void {
    const id = site?.id ?? null;
    if (!site || !id || id === this.appliedSiteIds[side]) {
      this.appliedSiteIds[side] = id;
      return;
    }
    this.appliedSiteIds[side] = id;

    const address = site.address;
    if (!address) {
      return;
    }
    const street = address.streetAddress || site.name || "";
    const pin: GeoPoint =
      site.latitude != null && site.longitude != null
        ? { latitude: site.latitude, longitude: site.longitude }
        : DUMMY_LOCATION;

    untracked(() =>
      this.model.update((v) =>
        side === "origin"
          ? {
              ...v,
              originAddress: { ...address, streetAddress: street },
              originStreetAddress: street,
              originSubdivision: address.subdivision ?? "",
              originLocation: pin,
            }
          : {
              ...v,
              destinationAddress: { ...address, streetAddress: street },
              destinationStreetAddress: street,
              destinationSubdivision: address.subdivision ?? "",
              destinationLocation: pin,
              requestedDeliveryDate:
                atTimeOfDay(
                  v.requestedDeliveryDate ?? v.requestedPickupDate,
                  site.defaultCutoffTime,
                ) ?? v.requestedDeliveryDate,
            },
      ),
    );
  }

  protected updateDistance(e: RouteChangeEvent): void {
    const miles = Converters.metersTo(e.distance, "mi");
    // Return the same object reference when the value is unchanged so the chained
    // computed signals (originWaypoint / destinationWaypoint / mapWaypoints) do
    // not re-emit new object references.  This breaks the draw() → routeChange
    // → updateDistance → model update → draw() feedback loop that would otherwise
    // keep the map loading indicator spinning indefinitely.
    this.model.update((v) => {
      if (v.distance === miles) return v;
      return { ...v, distance: miles };
    });
  }

  protected askRemove(): void {
    this.toastService.confirm({
      message: "Are you sure that you want to delete this load?",
      accept: () => this.remove.emit(),
    });
  }

  private toFormValue(): LoadFormValue {
    const v = this.model();

    const buildAddress = (addr: Address | null, street: string, subdivision: string): Address => ({
      streetAddress: street,
      subdivision: subdivision || null,
      cityOrMunicipality: addr?.cityOrMunicipality ?? "",
      province: addr?.province ?? "",
      region: addr?.region ?? "",
      zipcode: addr?.zipcode ?? "",
      country: addr?.country ?? "PH",
      barangay: addr?.barangay ?? null,
    });

    return {
      ...v,
      originAddress: buildAddress(v.originAddress, v.originStreetAddress, v.originSubdivision),
      destinationAddress: buildAddress(
        v.destinationAddress,
        v.destinationStreetAddress,
        v.destinationSubdivision,
      ),
      distance: Converters.toMeters(v.distance ?? 0, "mi"),
      assignedTruckId: v.assignedTruck?.id ?? null,
      requestedPickupDate: v.requestedPickupDate ? v.requestedPickupDate.toISOString() : null,
      requestedDeliveryDate: v.requestedDeliveryDate ? v.requestedDeliveryDate.toISOString() : null,
      containerId: v.container?.id ?? null,
      originTerminalId: v.originTerminal?.id ?? null,
      destinationTerminalId: v.destinationTerminal?.id ?? null,
      originSiteId: v.originSite?.id ?? null,
      destinationSiteId: v.destinationSite?.id ?? null,
      notes: v.notes || null,
      commodity: v.commodity || null,
      plannedBoxes: v.plannedBoxes,
      plannedBags: v.plannedBags,
      isHazmat: v.isHazmat,
      hazmatClass: v.isHazmat ? (v.hazmatClass ?? null) : null,
      unNumber: v.isHazmat ? v.unNumber || null : null,
    } as LoadFormValue;
  }

  private patch(src: Partial<LoadFormValue>): void {
    // Assign field by field rather than spreading `src`: `LoadFormValue` carries keys the model does
    // not have (`assignedTruckId`, `containerId`, `originTerminalId`, ...). `patchValue()` used to
    // drop them; a `model.update()` spread would add them to the form tree.
    // `assignedTruck` is deliberately absent - the form only ever receives an `assignedTruckId`, and
    // `app-search-truck` resolves that to the DTO through its `truckId` input. Sites work the same
    // way; marking their ids applied keeps the resolve from overwriting the load's saved address.
    this.appliedSiteIds.origin = src.originSiteId ?? null;
    this.appliedSiteIds.destination = src.destinationSiteId ?? null;
    this.model.update((v) => ({
      ...v,
      name: src.name ?? v.name,
      type: src.type ?? v.type,
      source: src.source ?? v.source,
      customer: src.customer ?? v.customer,
      originAddress: src.originAddress ?? v.originAddress,
      originStreetAddress: src.originAddress?.streetAddress ?? v.originStreetAddress,
      originSubdivision: src.originAddress?.subdivision ?? v.originSubdivision,
      originLocation: src.originLocation ?? v.originLocation,
      destinationAddress: src.destinationAddress ?? v.destinationAddress,
      destinationStreetAddress: src.destinationAddress?.streetAddress ?? v.destinationStreetAddress,
      destinationSubdivision: src.destinationAddress?.subdivision ?? v.destinationSubdivision,
      destinationLocation: src.destinationLocation ?? v.destinationLocation,
      deliveryCost: src.deliveryCost ?? v.deliveryCost,
      distance: src.distance ?? v.distance,
      status: src.status ?? v.status,
      assignedDispatcherId: src.assignedDispatcherId ?? v.assignedDispatcherId,
      assignedDispatcherName: src.assignedDispatcherName ?? v.assignedDispatcherName,
      tripId: src.tripId ?? null,
      tripNumber: src.tripNumber ?? null,
      requestedPickupDate: src.requestedPickupDate ? new Date(src.requestedPickupDate) : null,
      requestedDeliveryDate: src.requestedDeliveryDate ? new Date(src.requestedDeliveryDate) : null,
      container: src.container ?? null,
      originTerminal: src.originTerminal ?? null,
      destinationTerminal: src.destinationTerminal ?? null,
      hazmatClass: src.hazmatClass ?? null,
      notes: src.notes ?? "",
      commodity: src.commodity ?? "",
      plannedBoxes: src.plannedBoxes ?? null,
      plannedBags: src.plannedBags ?? null,
      unNumber: src.unNumber ?? "",
      isHazmat: src.isHazmat ?? false,
    }));
  }

  private setCurrentDispatcher(): void {
    const userData = this.authService.getUserData();

    if (userData) {
      this.model.update((v) => ({
        ...v,
        assignedDispatcherId: userData.id,
        assignedDispatcherName: userData.getFullName(),
      }));
    }
  }
}
