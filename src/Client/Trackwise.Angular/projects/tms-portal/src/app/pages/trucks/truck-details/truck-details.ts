import { CommonModule } from "@angular/common";
import { Component, computed, inject, input, signal, type OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import { Permission } from "@trackwise/shared";
import {
  Api,
  getDocuments,
  getTruckMasterDataById,
  type DailyGrossesDto,
  type DocumentDto,
  type DocumentType,
  type MonthlyGrossesDto,
  type TruckDto,
} from "@trackwise/shared/api";
import { truckOwnershipOptions } from "@trackwise/shared/api/enums";
import type { TruckGeolocationDto } from "@trackwise/shared/api/models";
import { AddressPipe, CurrencyFormatPipe, DistanceUnitPipe } from "@trackwise/shared/pipes";
import { LocalizationService, PermissionService } from "@trackwise/shared/services";
import {
  Card,
  CountBadge,
  DetailSection,
  Divider,
  EmptyState,
  Grid,
  Icon,
  InfoRow,
  Spinner,
  Stack,
  StatItem,
  Surface,
  Typography,
  UiButton,
  UiTabsImports,
} from "@trackwise/shared/ui";
import {
  DocumentManager,
  GeolocationMap,
  GrossBarchart,
  PageHeader,
  TruckMaintenance,
  TruckStatusTag,
  TruckTypeTag,
  VehiclePapers,
  type BarChartDrawnEvent,
} from "@/shared/components";
import { MasterDataHistory } from "../../master-data/components/master-data-history/master-data-history";
import {
  DocumentStatusOverview,
  TruckGrossLinechart,
  TruckLoadsList,
  type LineChartDrawnEvent,
} from "../components";

@Component({
  selector: "app-truck-details",
  templateUrl: "./truck-details.html",
  styleUrl: "./truck-details.css",
  imports: [
    AddressPipe,
    Card,
    CommonModule,
    CountBadge,
    CurrencyFormatPipe,
    DetailSection,
    DistanceUnitPipe,
    Divider,
    DocumentManager,
    DocumentStatusOverview,
    EmptyState,
    GeolocationMap,
    Grid,
    GrossBarchart,
    Icon,
    InfoRow,
    MasterDataHistory,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    StatItem,
    Surface,
    TruckGrossLinechart,
    TruckLoadsList,
    TruckStatusTag,
    TruckTypeTag,
    Typography,
    UiButton,
    UiTabsImports,
    TruckMaintenance,
    VehiclePapers,
  ],
})
export class TruckDetailsComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly localizationService = inject(LocalizationService, { optional: true });
  private readonly permissions = inject(PermissionService);

  protected readonly canViewLoads = this.permissions.hasPermission(Permission.Load.View);
  protected readonly canViewPapers = this.permissions.hasPermission(Permission.Truck.View);
  protected readonly canViewMaintenance = this.permissions.hasPermission(
    Permission.Maintenance.View,
  );
  protected readonly canViewAnalytics = this.permissions.hasPermission(Permission.Stat.View);

  protected readonly id = input<string>();

  protected readonly distanceUnitLabel = computed(
    () => this.localizationService?.getDistanceUnitLabel() ?? "mi",
  );
  protected readonly isLoading = signal(false);
  protected readonly truck = signal<TruckDto | null>(null);
  protected readonly dailyGrosses = signal<DailyGrossesDto | null>(null);
  protected readonly monthlyGrosses = signal<MonthlyGrossesDto | null>(null);
  protected readonly rpmCurrent = signal(0);
  protected readonly rpmAllTime = signal(0);
  protected readonly truckLocations = signal<TruckGeolocationDto[]>([]);
  protected readonly documents = signal<DocumentDto[]>([]);
  protected readonly activeTab = signal(0);

  protected readonly ownershipLabel = computed(
    () =>
      truckOwnershipOptions.find((o) => o.value === this.truck()?.ownership)?.label ??
      "Not confirmed",
  );

  protected readonly truckDocTypes: DocumentType[] = [
    "vehicle_registration",
    "insurance_certificate",
    "dot_inspection",
    "title_certificate",
    "lease_agreement",
    "maintenance_record",
    "annual_inspection",
    "photo",
    "other",
  ];

  ngOnInit(): void {
    this.fetchTruck();
    this.fetchDocuments();
  }

  onTabChange(index: unknown): void {
    this.activeTab.set(index as number);
  }

  onLineChartDrawn(event: LineChartDrawnEvent): void {
    this.dailyGrosses.set(event.dailyGrosses);
    this.rpmCurrent.set(event.rpm);
  }

  onBarChartDrawn(event: BarChartDrawnEvent): void {
    this.monthlyGrosses.set(event.monthlyGrosses);
    this.rpmAllTime.set(event.rpm);
  }

  private async fetchTruck(): Promise<void> {
    const id = this.id();

    if (!id) {
      return;
    }

    this.isLoading.set(true);

    const truck = await this.api.invoke(getTruckMasterDataById, { id });
    if (truck) {
      this.truck.set(truck);

      this.truckLocations.set([
        {
          currentLocation: truck.currentLocation,
          truckId: truck.id,
          truckNumber: truck.number,
          driversName: [truck.mainDriver?.fullName, truck.secondaryDriver?.fullName]
            .filter(Boolean)
            .join(", "),
        },
      ]);
    }

    this.isLoading.set(false);
  }

  private async fetchDocuments(): Promise<void> {
    const id = this.id();
    if (!id) {
      return;
    }

    const docs = await this.api.invoke(getDocuments, {
      OwnerType: "truck",
      OwnerId: id,
    });

    if (docs) {
      this.documents.set(docs);
    }
  }
}
