import { Component, computed, inject, input, model, output, signal } from "@angular/core";
import { form, FormField, FormRoot, validate } from "@angular/forms/signals";
import { getApiErrorMessage } from "@trackwise/shared";
import {
  addTruckLeg,
  Api,
  getBusinessPartners,
  getLocationById,
  getTerminalById,
  updateTruckLeg,
  type LocationDto,
  type TerminalDto,
  type TruckLegDto,
  type TruckLegRole,
  type WorkfileContainerDto,
} from "@trackwise/shared/api";
import { truckLegRoleOptions } from "@trackwise/shared/api/enums";
import {
  Grid,
  UiButton,
  UiDateField,
  UiDialog,
  UiFormField,
  UiMultiSelectField,
  UiSelectField,
  UiTextareaField,
  UiToggleGroup,
  ValidatedForm,
  type UiToggleOption,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { SearchLocation, SearchTerminal } from "@/shared/components/search";
import { dateToIso, isoToDate } from "@/shared/utils";

type EndKind = "port" | "site";

interface LegModel {
  role: TruckLegRole;
  fromKind: EndKind;
  fromTerminal: TerminalDto | null;
  fromSite: LocationDto | null;
  toKind: EndKind;
  toTerminal: TerminalDto | null;
  toSite: LocationDto | null;
  slotIds: string[];
  truckerId: string | null;
  pickupDate: Date | null;
  notes: string;
}

/** Which ends a role usually has - the user can still flip either one. */
const ROLE_ENDS: Record<TruckLegRole, [EndKind, EndKind]> = {
  pickup: ["site", "port"],
  delivery: ["port", "site"],
  transfer: ["port", "site"],
};

const END_OPTIONS: UiToggleOption<EndKind>[] = [
  { label: "Port", value: "port", icon: "ship" },
  { label: "Site", value: "site", icon: "warehouse" },
];

/**
 * Opens a truck leg on a forwarding workfile - a child SLC-TRK trucking file with one load per box -
 * or edits one whose loads haven't gone to dispatch. The leg is never billed on its own - the move
 * is billed once, on this workfile.
 */
@Component({
  selector: "app-workfile-leg-dialog",
  templateUrl: "./workfile-leg-dialog.html",
  imports: [
    FormField,
    FormRoot,
    Grid,
    SearchLocation,
    SearchTerminal,
    UiButton,
    UiDateField,
    UiDialog,
    UiFormField,
    UiMultiSelectField,
    UiSelectField,
    UiTextareaField,
    UiToggleGroup,
    ValidatedForm,
  ],
})
export class WorkfileLegDialog {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);

  public readonly workfileId = input.required<string>();
  public readonly customerId = input.required<string>();
  public readonly containers = input.required<WorkfileContainerDto[]>();
  /** Import files usually need a delivery leg, exports a pickup. */
  public readonly defaultRole = input<TruckLegRole>("delivery");
  /** The leg being edited; null to add one. */
  public readonly leg = input<TruckLegDto | null>(null);
  public readonly open = model(false);
  public readonly saved = output();

  protected readonly roleOptions = truckLegRoleOptions;
  protected readonly endOptions = END_OPTIONS;
  protected readonly truckerOptions = signal<{ label: string; value: string }[]>([]);

  protected readonly slotOptions = computed(() =>
    this.containers().map((slot, i) => ({
      label: slot.containerNumber ?? `${slot.containerTypeCode ?? "?"} #${i + 1}`,
      value: slot.id!,
    })),
  );

  // Seeded without the inputs, which aren't set yet while the field initialisers run; onOpened()
  // fills it for real each time the dialog opens.
  protected readonly model = signal<LegModel>(this.emptyModel([]));

  protected readonly form = form(
    this.model,
    (p) => {
      validate(p.fromTerminal, ({ value }) =>
        this.model().fromKind === "port" && !value()
          ? { kind: "required", message: "Pick the port the leg starts at." }
          : null,
      );
      validate(p.fromSite, ({ value }) =>
        this.model().fromKind === "site" && !value()
          ? { kind: "required", message: "Pick the site the leg starts at." }
          : null,
      );
      validate(p.toTerminal, ({ value }) =>
        this.model().toKind === "port" && !value()
          ? { kind: "required", message: "Pick the port the leg ends at." }
          : null,
      );
      validate(p.toSite, ({ value }) =>
        this.model().toKind === "site" && !value()
          ? { kind: "required", message: "Pick the site the leg ends at." }
          : null,
      );
    },
    {
      submission: {
        action: async () => {
          const v = this.model();
          const body = {
            role: v.role,
            from:
              v.fromKind === "port"
                ? { terminalId: v.fromTerminal!.id }
                : { siteId: v.fromSite!.id },
            to: v.toKind === "port" ? { terminalId: v.toTerminal!.id } : { siteId: v.toSite!.id },
            containerSlotIds: v.slotIds,
            truckerId: v.truckerId,
            pickupDate: dateToIso(v.pickupDate),
            notes: v.notes.trim() || null,
          };
          const leg = this.leg();
          try {
            if (leg) {
              await this.api.invoke(updateTruckLeg, {
                id: this.workfileId(),
                legId: leg.id!,
                body,
              });
              this.toast.showSuccess("Truck leg updated.");
            } else {
              await this.api.invoke(addTruckLeg, { id: this.workfileId(), body });
              this.toast.showSuccess("Truck leg added.");
            }
            this.open.set(false);
            this.saved.emit();
          } catch (error) {
            this.toast.showError(
              getApiErrorMessage(
                error,
                leg ? "Could not update the truck leg." : "Could not add the truck leg.",
              ),
            );
          }
          return undefined;
        },
      },
    },
  );

  protected setRole(role: TruckLegRole | null): void {
    if (!role) {
      return;
    }
    const [fromKind, toKind] = ROLE_ENDS[role];
    this.model.update((m) => ({ ...m, role, fromKind, toKind }));
  }

  protected setFromKind(kind: EndKind): void {
    this.model.update((m) => ({ ...m, fromKind: kind }));
  }

  protected setToKind(kind: EndKind): void {
    this.model.update((m) => ({ ...m, toKind: kind }));
  }

  protected async onOpened(): Promise<void> {
    const leg = this.leg();
    this.model.set(this.emptyModel(this.containers()));
    await Promise.all([this.loadTruckers(), leg ? this.loadLeg(leg) : Promise.resolve()]);
  }

  /** The search fields hold whole records, so the leg's ends are fetched by id. */
  private async loadLeg(leg: TruckLegDto): Promise<void> {
    try {
      const [fromTerminal, fromSite, toTerminal, toSite] = await Promise.all([
        this.terminal(leg.fromTerminalId),
        this.site(leg.fromSiteId),
        this.terminal(leg.toTerminalId),
        this.site(leg.toSiteId),
      ]);
      this.model.set({
        role: leg.role ?? this.defaultRole(),
        fromKind: leg.fromTerminalId ? "port" : "site",
        fromTerminal,
        fromSite,
        toKind: leg.toTerminalId ? "port" : "site",
        toTerminal,
        toSite,
        slotIds: leg.containerSlotIds ?? [],
        truckerId: leg.truckerId ?? null,
        pickupDate: isoToDate(leg.pickupDate),
        notes: leg.notes ?? "",
      });
    } catch (error) {
      this.toast.showError(getApiErrorMessage(error, "Could not load the truck leg."));
    }
  }

  private async terminal(id: string | null | undefined): Promise<TerminalDto | null> {
    return id ? this.api.invoke(getTerminalById, { id }) : null;
  }

  private async site(id: string | null | undefined): Promise<LocationDto | null> {
    return id ? this.api.invoke(getLocationById, { id }) : null;
  }

  private async loadTruckers(): Promise<void> {
    try {
      const result = await this.api.invoke(getBusinessPartners, {
        Role: "trucker",
        IsActive: true,
        OrderBy: "Name",
        PageSize: 100,
      });
      this.truckerOptions.set((result.items ?? []).map((p) => ({ label: p.name!, value: p.id! })));
    } catch {
      this.truckerOptions.set([]);
    }
  }

  private emptyModel(containers: WorkfileContainerDto[]): LegModel {
    const role = this.defaultRole();
    const [fromKind, toKind] = ROLE_ENDS[role];
    return {
      role,
      fromKind,
      fromTerminal: null,
      fromSite: null,
      toKind,
      toTerminal: null,
      toSite: null,
      slotIds: containers.map((slot) => slot.id!),
      truckerId: null,
      pickupDate: null,
      notes: "",
    };
  }
}
