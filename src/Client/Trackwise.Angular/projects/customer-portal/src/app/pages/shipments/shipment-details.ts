import { Component, effect, inject, input, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import {
  Api,
  getPortalLoad,
  getPortalLoadDocuments,
  type DocumentDto,
  type PortalLoadDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe, DateFormatPipe, DistanceUnitPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Grid,
  Icon,
  Spinner,
  Stack,
  Surface,
  Typography,
  UiButton,
  UiDataTable,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";

@Component({
  selector: "cp-shipment-details",
  templateUrl: "./shipment-details.html",
  imports: [
    Badge,
    CurrencyFormatPipe,
    DateFormatPipe,
    DistanceUnitPipe,
    Grid,
    Icon,
    RouterLink,
    Spinner,
    Stack,
    Surface,
    Typography,
    UiButton,
    UiDataTable,
  ],
})
export class ShipmentDetails {
  private readonly api = inject(Api);

  protected readonly id = input.required<string>();
  protected readonly load = signal<PortalLoadDto | null>(null);
  protected readonly documents = signal<DocumentDto[]>([]);
  protected readonly isLoading = signal(true);

  constructor() {
    effect(() => {
      const loadId = this.id();
      if (loadId) {
        this.loadData(loadId);
      }
    });
  }

  private async loadData(loadId: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const [loadData, docs] = await Promise.all([
        this.api.invoke(getPortalLoad, { loadId }),
        this.api.invoke(getPortalLoadDocuments, { loadId }),
      ]);

      this.load.set(loadData);
      this.documents.set(docs ?? []);
    } catch (error) {
      console.error("Failed to load shipment details:", error);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected getStatusSeverity(status: string | undefined): UiBadgeIntent {
    switch (status) {
      case "Delivered":
        return "success";
      case "PickedUp":
        return "info";
      case "Dispatched":
        return "warn";
      default:
        return "info";
    }
  }

  protected formatAddress(
    address:
      | {
          streetAddress?: string | null;
          cityOrMunicipality?: string | null;
          province?: string | null;
          zipcode?: string | null;
        }
      | undefined,
  ): string {
    if (!address) return "-";
    const parts = [
      address.streetAddress,
      address.cityOrMunicipality,
      address.province,
      address.zipcode,
    ].filter(Boolean);
    return parts.length > 0 ? parts.join(", ") : "-";
  }
}
