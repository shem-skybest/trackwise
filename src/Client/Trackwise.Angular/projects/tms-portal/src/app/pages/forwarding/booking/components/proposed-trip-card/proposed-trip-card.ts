import { Component, computed, input, output } from "@angular/core";
import type { ProposedColdChainTripDto, ProposedSiteDto } from "@trackwise/shared/api";
import { Badge, Card, Icon, Stack, Typography, UiButton } from "@trackwise/shared/ui";
import { truckSizeLabel } from "../../utils/booking-labels";

export interface DropSitePick {
  dropIndex: number;
  site: ProposedSiteDto | null;
}

/**
 * One truck the AI read from a client's booking request: the plan, and each drop as the client
 * wrote it beside the saved location it matched. An unsure drop shows its likely locations to pick.
 */
@Component({
  selector: "app-proposed-trip-card",
  templateUrl: "./proposed-trip-card.html",
  imports: [Badge, Card, Icon, Stack, Typography, UiButton],
})
export class ProposedTripCard {
  public readonly trip = input.required<ProposedColdChainTripDto>();
  public readonly truckNumber = input.required<number>();
  public readonly sitePicked = output<DropSitePick>();

  protected readonly serviceDate = computed(() => formatDay(this.trip().serviceDate));
  protected readonly callTime = computed(() => this.trip().callTime?.slice(0, 5) ?? null);
  protected readonly truckSize = computed(() =>
    this.trip().truckSize ? truckSizeLabel(this.trip().truckSize) : null,
  );
  protected readonly temperature = computed(() => {
    const { temperatureMinC: min, temperatureMaxC: max } = this.trip();
    if (min == null && max == null) return null;
    if (min != null && max != null) return `${min} to ${max} °C`;
    return `${min ?? max} °C`;
  });
  protected candidateLabel(site: ProposedSiteDto): string {
    const name = site.name ?? "Unnamed location";
    return site.city ? `${name} · ${site.city}` : name;
  }

  protected readonly unmatchedDrops = computed(
    () => (this.trip().drops ?? []).filter((d) => !d.site).length,
  );
}

/** "2026-09-29" -> "Tue, Sep 29, 2026", read as a calendar day so no time zone can shift it. */
function formatDay(value: string | null | undefined): string | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value ?? "");
  if (!match) return null;
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
  return date.toLocaleDateString("en-PH", {
    weekday: "short",
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}
