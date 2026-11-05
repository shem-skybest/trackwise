import { DatePipe } from "@angular/common";
import { Component, inject, input, signal, type OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import {
  Api,
  getMaintenanceRequisition,
  type MaintenanceRequisitionDto,
} from "@trackwise/shared/api";
import { CurrencyFormatPipe } from "@trackwise/shared/pipes";
import { Card, Spinner, Stack, Typography } from "@trackwise/shared/ui";
import { PageHeader } from "@/shared/components";

/**
 * Every truck's service records paid through one request for payment, totalled - to check
 * against the nSFS RFP-RFV it was paid on.
 */
@Component({
  selector: "app-requisition-details",
  templateUrl: "./requisition-details.html",
  imports: [Card, CurrencyFormatPipe, DatePipe, PageHeader, RouterLink, Spinner, Stack, Typography],
})
export class RequisitionDetailsPage implements OnInit {
  private readonly api = inject(Api);

  public readonly number = input.required<string>();

  protected readonly isLoading = signal(true);
  protected readonly requisition = signal<MaintenanceRequisitionDto | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      this.requisition.set(
        await this.api.invoke(getMaintenanceRequisition, { number: this.number() }),
      );
    } catch {
      this.requisition.set(null);
    } finally {
      this.isLoading.set(false);
    }
  }
}
