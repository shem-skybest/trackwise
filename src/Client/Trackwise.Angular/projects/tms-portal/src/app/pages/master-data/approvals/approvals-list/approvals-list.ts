import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import type { MasterDataApprovalDto } from "@trackwise/shared/api";
import type { ListLazyLoadEvent } from "@trackwise/shared/stores";
import { Badge, Card, Stack, UiDataTable, UiFormField, UiSelectField } from "@trackwise/shared/ui";
import { DataContainer, PageHeader } from "@/shared/components";
import { ApprovalsListStore } from "../store/approvals-list.store";

/**
 * Where an approvable record lives, by entity type. The queue is entity-agnostic on the server -
 * one table, one query - so this map is the only thing that knows partners are at
 * /master-data/partners, and rates join it by gaining a line.
 */
const RECORD_ROUTE: Record<string, string> = {
  BusinessPartner: "/master-data/partners",
};

const ENTITY_LABELS: Record<string, string> = {
  BusinessPartner: "Business partner",
};

@Component({
  selector: "app-approvals-list",
  templateUrl: "./approvals-list.html",
  providers: [ApprovalsListStore],
  imports: [
    Badge,
    Card,
    DataContainer,
    DatePipe,
    PageHeader,
    Stack,
    UiDataTable,
    UiFormField,
    UiSelectField,
  ],
})
export class ApprovalsListComponent {
  private readonly router = inject(Router);

  protected readonly store = inject(ApprovalsListStore);

  protected readonly showingOpen = signal(true);

  protected readonly queueOptions = [
    { label: "Waiting for a decision", value: true },
    { label: "Already decided", value: false },
  ];

  constructor() {
    this.store.setFilters({ Open: true }, { reload: false });
  }

  protected onShowOpenChange(open: boolean | null): void {
    this.showingOpen.set(open ?? true);
    this.store.setFilters({ Open: open ?? true });
  }

  protected entityLabel(approval: MasterDataApprovalDto): string {
    return ENTITY_LABELS[approval.entityType ?? ""] ?? approval.entityType ?? "Record";
  }

  protected onLazyLoad(event: ListLazyLoadEvent): void {
    this.store.onLazyLoad(event);
  }

  protected open(approval: MasterDataApprovalDto): void {
    const base = RECORD_ROUTE[approval.entityType ?? ""];

    if (base && approval.entityId) {
      void this.router.navigateByUrl(`${base}/${approval.entityId}`);
    }
  }
}
