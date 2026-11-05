import { DatePipe } from "@angular/common";
import { Component, effect, inject, input, signal } from "@angular/core";
import {
  Api,
  getBranchHistory,
  getCommodityHistory,
  getContainerTypeHistory,
  getCurrencyHistory,
  getEmployeeHistory,
  getGensetHistory,
  getHelperHistory,
  getLocationHistory,
  getPartnerHistory,
  getPortHistory,
  getTrailerHistory,
  getTruckHistory,
  getVesselHistory,
  type MasterDataChangeLogDto,
  type MasterDataChangeType,
} from "@trackwise/shared/api";
import {
  Badge,
  Card,
  Icon,
  Spinner,
  Stack,
  Typography,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";

// Keyed by the value the API actually sends. MasterDataChangeType serialises snake_case, so the
// PascalCase keys this map shipped with never matched and every entry rendered grey.
const CHANGE_SEVERITY: Record<MasterDataChangeType, UiBadgeIntent> = {
  created: "success",
  updated: "info",
  deleted: "danger",
  restored: "warn",
  merged: "warn",
  imported: "secondary",
  submitted: "info",
  approved: "success",
  rejected: "danger",
};

/**
 * Which entity's history to read. The change log itself is keyed by entity type and id rather than
 * a foreign key per table, so this picks a route, not a different shape of data.
 */
export type MasterDataHistoryEntity =
  | "branch"
  | "port"
  | "vessel"
  | "commodity"
  | "location"
  | "containerType"
  | "currency"
  | "partner"
  | "trailer"
  | "helper"
  | "genset"
  | "truck"
  | "employee";

const HISTORY_ENDPOINT = {
  branch: getBranchHistory,
  port: getPortHistory,
  vessel: getVesselHistory,
  commodity: getCommodityHistory,
  location: getLocationHistory,
  containerType: getContainerTypeHistory,
  currency: getCurrencyHistory,
  partner: getPartnerHistory,
  trailer: getTrailerHistory,
  helper: getHelperHistory,
  genset: getGensetHistory,
  truck: getTruckHistory,
  employee: getEmployeeHistory,
} as const;

@Component({
  selector: "app-master-data-history",
  templateUrl: "./master-data-history.html",
  imports: [Badge, Card, DatePipe, Icon, Spinner, Stack, Typography],
})
export class MasterDataHistory {
  private readonly api = inject(Api);

  public readonly entity = input.required<MasterDataHistoryEntity>();
  public readonly recordId = input.required<string>();

  /**
   * Bumped by the parent after a save. An edit writes new log rows, so the panel has to re-read or
   * it shows the trail as it stood before the change the user just made.
   */
  public readonly version = input(0);

  protected readonly entries = signal<MasterDataChangeLogDto[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly total = signal(0);

  constructor() {
    effect(() => {
      // All three are read so the effect re-runs on any of them.
      const entity = this.entity();
      const id = this.recordId();
      this.version();

      void this.load(entity, id);
    });
  }

  private async load(entity: MasterDataHistoryEntity, recordId: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const response = await this.api.invoke(HISTORY_ENDPOINT[entity], {
        id: recordId,
        page: 1,
        pageSize: 50,
      });

      this.entries.set(response.items ?? []);
      this.total.set(response.pagination?.total ?? 0);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected severity(changeType: MasterDataChangeType | undefined): UiBadgeIntent {
    return changeType ? (CHANGE_SEVERITY[changeType] ?? "secondary") : "secondary";
  }

  /** Blanks render as an em dash so an emptied field is visibly distinct from an unchanged one. */
  protected display(value: string | null | undefined): string {
    return value === null || value === undefined || value === "" ? "—" : value;
  }
}
