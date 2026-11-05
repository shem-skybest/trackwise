import { Component, input } from "@angular/core";
import { RouterLink } from "@angular/router";
import type { LoadStatus, TruckLegLoadDto, TruckLegRole } from "@trackwise/shared/api";
import { loadStatusOptions, truckLegRoleOptions } from "@trackwise/shared/api/enums";
import { Badge, Card, Stack, Typography, UiDataTable } from "@trackwise/shared/ui";

/**
 * A truck leg's side of the workfile page: the forwarding file it belongs to, and the loads that
 * do the work - one per box, dispatched like any other load. Leg loads carry no invoice.
 */
@Component({
  selector: "app-workfile-leg-loads",
  templateUrl: "./workfile-leg-loads.html",
  imports: [Badge, Card, RouterLink, Stack, Typography, UiDataTable],
})
export class WorkfileLegLoads {
  public readonly parentId = input.required<string>();
  public readonly parentReference = input<string | null | undefined>(null);
  public readonly role = input<TruckLegRole | null | undefined>(null);
  public readonly loads = input.required<TruckLegLoadDto[]>();

  protected roleLabel(): string {
    return truckLegRoleOptions.find((o) => o.value === this.role())?.label ?? "Truck leg";
  }

  protected statusLabel(status: LoadStatus | undefined): string {
    return loadStatusOptions.find((o) => o.value === status)?.label ?? "-";
  }
}
