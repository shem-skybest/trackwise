import { DatePipe } from "@angular/common";
import { Component, computed, inject, signal, type OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import {
  Api,
  getVehiclePermits,
  type VehiclePermitDto,
  type VehiclePermitType,
} from "@trackwise/shared/api";
import { vehiclePermitTypeOptions } from "@trackwise/shared/api/enums";
import {
  Badge,
  Card,
  Grid,
  Stack,
  StatItem,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSelectField,
} from "@trackwise/shared/ui";
import {
  PageHeader,
  permitExpiry,
  permitTypeLabel,
  VehiclePermitDialog,
} from "@/shared/components";

/** Window choices: `null` lists every current paper, however far off its expiry. */
const WINDOWS: { label: string; value: number | null }[] = [
  { label: "Due within 60 days", value: 60 },
  { label: "Due within 30 days", value: 30 },
  { label: "Expired", value: -1 },
  { label: "All current papers", value: null },
];

/**
 * Every active truck's and trailer's current papers, soonest expiry first - the renewal queue the
 * admin team used to keep on the vehicle register board.
 */
@Component({
  selector: "app-vehicle-papers-list",
  templateUrl: "./vehicle-papers-list.html",
  imports: [
    Badge,
    Card,
    DatePipe,
    Grid,
    PageHeader,
    RouterLink,
    Stack,
    StatItem,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSelectField,
    VehiclePermitDialog,
  ],
})
export class VehiclePapersList implements OnInit {
  private readonly api = inject(Api);

  protected readonly windows = WINDOWS;
  protected readonly typeOptions: { label: string; value: VehiclePermitType | null }[] = [
    { label: "All papers", value: null },
    ...vehiclePermitTypeOptions,
  ];

  protected readonly permits = signal<VehiclePermitDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly window = signal<number | null>(60);
  protected readonly type = signal<VehiclePermitType | null>(null);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<VehiclePermitDto | null>(null);

  protected readonly rows = computed(() => {
    const window = this.window();
    const type = this.type();
    return this.permits()
      .filter((p) => !type || p.type === type)
      .filter((p) => window === null || (p.daysUntilExpiry ?? 0) <= window)
      .map((permit) => ({
        permit,
        typeLabel: permitTypeLabel(permit),
        expiry: permitExpiry(permit),
      }));
  });

  protected readonly expiredCount = computed(
    () => this.permits().filter((p) => (p.daysUntilExpiry ?? 0) < 0).length,
  );
  protected readonly due30Count = computed(
    () => this.permits().filter((p) => this.dueWithin(p, 0, 30)).length,
  );
  protected readonly due60Count = computed(
    () => this.permits().filter((p) => this.dueWithin(p, 31, 60)).length,
  );

  ngOnInit(): void {
    void this.refresh();
  }

  protected vehicleLink(permit: VehiclePermitDto): unknown[] {
    return permit.truckId
      ? ["/master-data/trucks", permit.truckId]
      : ["/master-data/trailers", permit.trailerId];
  }

  protected openEdit(permit: VehiclePermitDto): void {
    this.editing.set(permit);
    this.dialogVisible.set(true);
  }

  protected async refresh(): Promise<void> {
    this.isLoading.set(true);
    try {
      // The whole fleet's current papers are a few hundred rows at most, so the window and type
      // filters work on the loaded list instead of refetching on every change.
      this.permits.set((await this.api.invoke(getVehiclePermits, {})) ?? []);
    } finally {
      this.isLoading.set(false);
    }
  }

  private dueWithin(permit: VehiclePermitDto, from: number, to: number): boolean {
    const days = permit.daysUntilExpiry ?? 0;
    return days >= from && days <= to;
  }
}
