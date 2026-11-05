import { CurrencyPipe, DatePipe, TitleCasePipe, UpperCasePipe } from "@angular/common";
import { Component, inject, input, signal, type OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import {
  Api,
  getTariffById,
  removeTariffRate,
  type TariffDto,
  type TariffRateDto,
} from "@trackwise/shared/api";
import {
  Card,
  ConfirmDeleteDialog,
  CountBadge,
  EmptyState,
  Grid,
  Icon,
  InfoItem,
  Spinner,
  Stack,
  Typography,
  UiButton,
  UiDataTable,
  UiTabsImports,
} from "@trackwise/shared/ui";
import { ToastService } from "@/core/services";
import { PageHeader } from "@/shared/components";

@Component({
  selector: "app-tariff-details",
  templateUrl: "./tariff-details.html",
  imports: [
    Card,
    CountBadge,
    CurrencyPipe,
    DatePipe,
    EmptyState,
    Grid,
    Icon,
    InfoItem,
    PageHeader,
    RouterLink,
    Spinner,
    Stack,
    TitleCasePipe,
    Typography,
    UiButton,
    UiDataTable,
    UiTabsImports,
    UpperCasePipe,
    ConfirmDeleteDialog,
  ],
})
export class TariffDetails implements OnInit {
  private readonly api = inject(Api);
  private readonly toastService = inject(ToastService);

  public readonly id = input.required<string>();
  protected readonly isLoading = signal(true);
  protected readonly tariff = signal<TariffDto | null>(null);

  protected readonly toDeleteRate = signal<TariffRateDto | null>(null);
  protected readonly deleteRateConfirmVisible = signal(false);
  protected readonly isDeletingLine = signal(false);

  ngOnInit(): void {
    void this.loadTariff();
  }

  protected async loadTariff(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.tariff.set(await this.api.invoke(getTariffById, { id: this.id() }));
    } catch {
      this.toastService.showError("Failed to load tariff");
    } finally {
      this.isLoading.set(false);
    }
  }

  protected promptDeleteRate(rate: TariffRateDto): void {
    this.toDeleteRate.set(rate);

    this.deleteRateConfirmVisible.set(true);
  }

  protected async onConfirmDeleteRate(): Promise<void> {
    if (this.toDeleteRate()?.id == null) return;

    this.isDeletingLine.set(true);

    try {
      await this.api.invoke(removeTariffRate, {
        rateId: this.toDeleteRate()?.id!,
      });

      this.deleteRateConfirmVisible.set(false);
      this.toDeleteRate.set(null);
      this.loadTariff();
    } finally {
      this.isDeletingLine.set(false);
    }
  }
}
