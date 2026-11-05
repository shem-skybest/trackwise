import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { CashFundDto, CashFundStatus } from "@trackwise/shared/api";
import { cashFundStatusOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import {
  Badge,
  Card,
  Grid,
  Stack,
  UiButton,
  UiDataTable,
  UiFormField,
  UiSelectField,
  UiSortHeader,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { DataContainer, PageHeader } from "@/shared/components";
import { isoToDate } from "@/shared/utils";
import { CashFundDialog } from "../components/cash-fund-dialog/cash-fund-dialog";
import { CashFundsListStore } from "../store/cash-funds-list.store";
import { fundStatusIntent, fundStatusLabel } from "../utils/cash-fund-labels";

/**
 * Operational funds, newest period first - what Monday's CCL OPERATION FUNDS LIQUIDATION board
 * grouped by half-month.
 */
@Component({
  selector: "app-cash-funds-list",
  templateUrl: "./cash-funds-list.html",
  providers: [CashFundsListStore],
  imports: [
    Badge,
    Card,
    CashFundDialog,
    CurrencyFormatPipe,
    DataContainer,
    DatePipe,
    Grid,
    PageHeader,
    RouterLink,
    Stack,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSelectField,
    UiSortHeader,
  ],
})
export class CashFundsList {
  private readonly router = inject(Router);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(CashFundsListStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Expense.Manage);
  protected readonly status = signal<CashFundStatus | null>(null);
  protected readonly dialogOpen = signal(false);

  protected readonly statusOptions = cashFundStatusOptions;
  protected readonly statusLabel = fundStatusLabel;
  protected readonly statusIntent = fundStatusIntent;
  protected readonly isoToDate = isoToDate;

  protected onStatusChange(status: CashFundStatus | null): void {
    this.status.set(status);
    this.store.setFilters({ Status: status ?? undefined });
  }

  protected open(fund: CashFundDto): void {
    void this.router.navigate(["/cash-funds", fund.id]);
  }
}
