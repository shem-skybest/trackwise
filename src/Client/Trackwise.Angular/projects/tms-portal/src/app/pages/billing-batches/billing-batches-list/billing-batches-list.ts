import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { BillingBatchDto, BillingBatchStatus } from "@trackwise/shared/api";
import { billingBatchStatusOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Grid,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSelectField,
  UiSortHeader,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { DataContainer, PageHeader } from "@/shared/components";
import { isoToDate } from "@/shared/utils";
import {
  BillingBatchDialog,
  type BillingBatchPrefill,
} from "../components/billing-batch-dialog/billing-batch-dialog";
import { UnbilledTripsPanel } from "../components/unbilled-trips-panel/unbilled-trips-panel";
import { BillingBatchesListStore } from "../store/billing-batches-list.store";
import { batchStatusIntent, batchStatusLabel } from "../utils/billing-batch-labels";

/**
 * Billing batches, newest period first - Monday's MONTHLY BILLING UPDATES - with what's left to
 * bill above them.
 */
@Component({
  selector: "app-billing-batches-list",
  templateUrl: "./billing-batches-list.html",
  providers: [BillingBatchesListStore],
  imports: [
    Badge,
    BillingBatchDialog,
    Card,
    CurrencyFormatPipe,
    DataContainer,
    DatePipe,
    Grid,
    PageHeader,
    RouterLink,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSelectField,
    UiSortHeader,
    UnbilledTripsPanel,
  ],
})
export class BillingBatchesList {
  private readonly router = inject(Router);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(BillingBatchesListStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Invoice.Manage);
  protected readonly status = signal<BillingBatchStatus | null>(null);
  protected readonly dialogOpen = signal(false);
  protected readonly prefill = signal<BillingBatchPrefill | null>(null);

  protected readonly statusOptions = billingBatchStatusOptions;
  protected readonly statusLabel = batchStatusLabel;
  protected readonly statusIntent = batchStatusIntent;
  protected readonly isoToDate = isoToDate;

  protected onStatusChange(status: BillingBatchStatus | null): void {
    this.status.set(status);
    this.store.setFilters({ Status: status ?? undefined });
  }

  protected newBatch(prefill: BillingBatchPrefill | null = null): void {
    this.prefill.set(prefill);
    this.dialogOpen.set(true);
  }

  protected open(batch: BillingBatchDto): void {
    void this.router.navigate(["/billing-batches", batch.id]);
  }
}
