import { DatePipe } from "@angular/common";
import { Component, inject, input, output } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { AppError } from "@trackwise/shared";
import type { BusinessPartnerDto, BusinessPartnerRole } from "@trackwise/shared/api";
import { businessPartnerRoleOptions } from "@trackwise/shared/api/enums";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import {
  Badge,
  Card,
  UiButton,
  UiDataTable,
  UiSortHeader,
  type UiBadgeIntent,
} from "@trackwise/shared/ui";
import { DataContainer } from "@/shared/components";

const ROLE_LABELS = new Map(
  businessPartnerRoleOptions.map((option) => [option.value, option.label]),
);

/**
 * One badge per lifecycle state, read off the server-derived `state` rather than reassembled from
 * the flags here. Draft and Awaiting approval are the two this entity added: both are inactive, and
 * a client that collapsed them into "Inactive" would hide the queue from the person who filled it.
 */
const STATE_BADGES: Record<string, { label: string; severity: UiBadgeIntent }> = {
  draft: { label: "Draft", severity: "secondary" },
  pending: { label: "Awaiting approval", severity: "warn" },
  active: { label: "Active", severity: "success" },
  inactive: { label: "Inactive", severity: "secondary" },
  deleted: { label: "Deleted", severity: "danger" },
  merged: { label: "Merged", severity: "warn" },
};

@Component({
  selector: "app-partners-table",
  templateUrl: "./partners-table.html",
  imports: [Badge, Card, DataContainer, DatePipe, RouterLink, UiButton, UiDataTable, UiSortHeader],
})
export class PartnersTable {
  private readonly router = inject(Router);

  public readonly data = input.required<BusinessPartnerDto[]>();
  public readonly isLoading = input(false);
  public readonly error = input<AppError | null>(null);
  public readonly isEmpty = input(false);
  public readonly totalRecords = input(0);
  public readonly pageSize = input(25);
  public readonly first = input(0);
  public readonly canManage = input(false);

  public readonly lazyLoad = output<ListLazyLoadEvent>();
  public readonly retry = output<void>();
  public readonly addPartner = output<void>();
  public readonly restorePartner = output<BusinessPartnerDto>();

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.lazyLoad.emit(event);
  }

  protected onRetry(): void {
    this.retry.emit();
  }

  protected onAddPartner(): void {
    this.addPartner.emit();
  }

  protected roleLabels(roles: BusinessPartnerRole[] | undefined): string {
    return (roles ?? []).map((role) => ROLE_LABELS.get(role) ?? role).join(", ") || "—";
  }

  protected stateLabel(partner: BusinessPartnerDto): string {
    return STATE_BADGES[partner.state ?? ""]?.label ?? "Unknown";
  }

  protected stateSeverity(partner: BusinessPartnerDto): UiBadgeIntent {
    return STATE_BADGES[partner.state ?? ""]?.severity ?? "secondary";
  }

  protected navigateToPartner(partner: BusinessPartnerDto): void {
    // A deleted partner has no edit form - restore is the only action available on it.
    if (partner.id && !partner.deletedAt) {
      this.router.navigateByUrl(`/master-data/partners/${partner.id}`);
    }
  }

  protected onRestore(partner: BusinessPartnerDto, event: Event): void {
    event.stopPropagation();
    this.restorePartner.emit(partner);
  }
}
