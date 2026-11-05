import { Component, computed, inject, input, signal, type OnInit } from "@angular/core";
import { Router } from "@angular/router";
import {
  Api,
  createTruckMasterData,
  deleteTruckMasterData,
  getTruckById,
  updateTruckMasterData,
  type CreateTruckCommand as CreateTruckMasterDataCommand,
  type TruckDto,
  type UpdateTruckCommand as UpdateTruckMasterDataCommand,
} from "@trackwise/shared/api";
import { Spinner, Stack } from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";
import { MasterDataHistory } from "../../master-data/components/master-data-history/master-data-history";
import { TruckForm, type TruckFormData } from "../components";

/**
 * The API's answer when the dependency probe stops a retirement. It carries the counts the steward
 * needs, so the message is shown verbatim rather than replaced with something generic.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

@Component({
  selector: "app-truck-edit",
  templateUrl: "./truck-edit.html",
  imports: [MasterDataHistory, PageHeader, Spinner, Stack, TruckForm],
})
export class TruckEdit implements OnInit {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  public readonly id = input<string | null>(null);
  protected readonly truck = signal<TruckDto | null>(null);
  protected readonly isLoading = signal(false);

  /** Bumped after a save so the history panel re-reads instead of showing a stale trail. */
  protected readonly historyVersion = signal(0);

  protected readonly mode = computed(() => (this.id() ? "edit" : "create") as "create" | "edit");
  protected readonly pageTitle = computed(() =>
    this.id() ? `Edit Truck #${this.truck()?.number ?? ""}` : "Add New Truck",
  );

  ngOnInit(): void {
    const id = this.id();
    if (id) {
      this.fetchTruck(id);
    }
  }

  protected async onSave(data: TruckFormData): Promise<void> {
    if (this.id()) {
      await this.updateTruck(data);
    } else {
      await this.createTruck(data);
    }
  }

  protected async onRemove(): Promise<void> {
    await this.deleteTruck();
  }

  private async fetchTruck(id: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const truck = await this.api.invoke(getTruckById, { truckOrDriverId: id });
      this.truck.set(truck);
    } finally {
      this.isLoading.set(false);
    }
  }

  private async createTruck(data: TruckFormData): Promise<void> {
    this.isLoading.set(true);
    try {
      const command: CreateTruckMasterDataCommand = {
        number: data.truckNumber,
        name: data.name ?? undefined,
        type: data.truckType,
        mainDriverId: data.mainDriver?.id ?? undefined,
        vehicleCapacity: data.vehicleCapacity,
        tonnageClass: data.tonnageClass ?? undefined,
        grossVehicleWeightKg: data.grossVehicleWeightKg,
        make: data.make,
        model: data.model,
        year: data.year,
        vin: data.vin,
        licensePlate: data.licensePlate,
        licensePlateState: data.licensePlateState,
        assetCode: data.assetCode,
        conductionSticker: data.conductionSticker,
        engineNumber: data.engineNumber,
        acquiredOn: data.acquiredOn,
        supplier: data.supplier,
        ownership: data.ownership ?? undefined,
        branchId: data.branchId,
      };

      const created = await this.api.invoke(createTruckMasterData, { body: command });
      this.toastService.showSuccess("Truck has been created successfully");
      this.router.navigateByUrl(
        created?.id ? `/master-data/trucks/${created.id}` : "/master-data/trucks",
      );
    } catch (error) {
      this.toastService.showError(
        this.conflictMessage(error) ??
          "Failed to create truck. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  private async updateTruck(data: TruckFormData): Promise<void> {
    this.isLoading.set(true);
    try {
      const command: UpdateTruckMasterDataCommand = {
        name: data.name ?? this.truck()?.name ?? data.truckNumber,
        type: data.truckType,
        status: data.truckStatus,
        mainDriverId: data.mainDriver?.id,
        secondaryDriverId: data.secondaryDriver?.id,
        vehicleCapacity: data.vehicleCapacity,
        tonnageClass: data.tonnageClass ?? undefined,
        grossVehicleWeightKg: data.grossVehicleWeightKg,
        make: data.make ?? undefined,
        model: data.model ?? undefined,
        year: data.year ?? undefined,
        vin: data.vin ?? undefined,
        licensePlate: data.licensePlate ?? undefined,
        licensePlateState: data.licensePlateState ?? undefined,
        assetCode: data.assetCode,
        conductionSticker: data.conductionSticker,
        engineNumber: data.engineNumber,
        acquiredOn: data.acquiredOn,
        supplier: data.supplier,
        ownership: data.ownership ?? undefined,
        adrEquipment: data.adrEquipment,
        isHazmatPlacarded: data.isHazmatPlacarded,
        branchId: data.branchId,
        isActive: data.isActive,
      };

      await this.api.invoke(updateTruckMasterData, {
        id: this.id()!,
        body: command,
      });
      this.toastService.showSuccess("Truck has been updated successfully");
      await this.fetchTruck(this.id()!);
      this.historyVersion.update((v) => v + 1);
    } catch (error) {
      this.toastService.showError(
        this.conflictMessage(error) ??
          "Failed to update truck. Please check the information and try again.",
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  private async deleteTruck(): Promise<void> {
    if (!this.id()) {
      return;
    }

    this.isLoading.set(true);
    try {
      await this.api.invoke(deleteTruckMasterData, { id: this.id()! });
      this.toastService.showSuccess("Truck deleted. It can be restored from the trucks list.");
      // A deleted truck no longer resolves through this page's read (the soft-delete filter
      // hides it, same as every other master-data entity) - restore lives on the list, not here.
      this.router.navigateByUrl("/master-data/trucks");
    } catch (error) {
      this.toastService.showError(this.conflictMessage(error) ?? "Failed to delete truck.");
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * The dependency probe's own message, which names what is still using the truck and how much of
   * it. Overriding it is a deliberate, recorded act and stays API-only for now, so there is no
   * one-click "delete anyway" here to make it look casual.
   */
  private conflictMessage(error: unknown): string | null {
    const body = (error as { error?: { errorCode?: string; message?: string } })?.error;

    return body?.errorCode === RECORD_IN_USE ? (body.message ?? null) : null;
  }
}
