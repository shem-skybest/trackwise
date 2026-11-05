import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import type { CustomerDto, RateAgreementDto, RateAgreementStatus } from "@trackwise/shared/api";
import { rateAgreementStatusOptions } from "@trackwise/shared/api/enums";
import { Permission } from "@trackwise/shared/models/permissions";
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
  UiTextField,
  UiToggleField,
} from "@trackwise/shared/ui";
import { PermissionService } from "@/core/auth";
import { DataContainer, PageHeader } from "@/shared/components";
import { SearchCustomer } from "@/shared/components/search";
import { dateToIso, isoToDate } from "@/shared/utils";
import { DieselPriceCard } from "../components/diesel-price-card/diesel-price-card";
import { RateAgreementsListStore } from "../store/rate-agreements-list.store";
import { fuelMethodLabel, statusIntent, statusLabel } from "../utils/rate-agreement-labels";

@Component({
  selector: "app-rate-agreements-list",
  templateUrl: "./rate-agreements-list.html",
  providers: [RateAgreementsListStore],
  imports: [
    Badge,
    Card,
    DataContainer,
    DatePipe,
    DieselPriceCard,
    Grid,
    PageHeader,
    RouterLink,
    SearchCustomer,
    Stack,
    UiButton,
    UiDataTable,
    UiFormField,
    UiSelectField,
    UiSortHeader,
    UiTextField,
    UiToggleField,
  ],
})
export class RateAgreementsList {
  private readonly router = inject(Router);
  private readonly permissions = inject(PermissionService);

  protected readonly store = inject(RateAgreementsListStore);
  protected readonly canManage = this.permissions.hasPermission(Permission.Tariff.Manage);

  protected readonly customer = signal<CustomerDto | null>(null);
  protected readonly status = signal<RateAgreementStatus | null>(null);

  /** On by default: what a person usually wants is today's rates, not every past revision. */
  protected readonly inForceToday = signal(true);

  protected readonly statusOptions = [
    { label: "All statuses", value: null },
    ...rateAgreementStatusOptions,
  ];

  protected readonly statusLabel = statusLabel;
  protected readonly statusIntent = statusIntent;
  protected readonly fuelMethodLabel = fuelMethodLabel;
  protected readonly isoToDate = isoToDate;

  constructor() {
    this.store.setFilters({ InForceOn: dateToIso(new Date()) }, { reload: false });
  }

  protected onSearch(value: string): void {
    this.store.setSearch(value);
  }

  protected onCustomerChange(customer: CustomerDto | null): void {
    this.customer.set(customer);
    this.store.setFilters({ CustomerId: customer?.id ?? undefined });
  }

  protected onStatusChange(status: RateAgreementStatus | null): void {
    this.status.set(status);
    this.store.setFilters({ Status: status ?? undefined });
  }

  protected onInForceChange(value: boolean): void {
    this.inForceToday.set(value);
    this.store.setFilters({ InForceOn: value ? dateToIso(new Date()) : undefined });
  }

  protected open(agreement: RateAgreementDto): void {
    void this.router.navigate(["/rate-agreements", agreement.id]);
  }
}
