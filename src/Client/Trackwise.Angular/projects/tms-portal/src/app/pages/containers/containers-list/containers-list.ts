import { DatePipe } from "@angular/common";
import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { Permission } from "@trackwise/shared";
import type { ContainerDto, ContainerStatus } from "@trackwise/shared/api";
import { containerStatusOptions } from "@trackwise/shared/api/enums";
import {
  Card,
  Icon,
  Stack,
  StatusBadge,
  UiButton,
  UiDataTable,
  UiSelectField,
  UiSortHeader,
} from "@trackwise/shared/ui";
import { ContainerTypeOptionsService } from "@/core/services";
import { DataContainer, PageHeader, SearchField } from "@/shared/components";
import { ContainersListStore } from "../store";

@Component({
  selector: "app-containers-list",
  templateUrl: "./containers-list.html",
  providers: [ContainersListStore],
  imports: [
    Card,
    DataContainer,
    DatePipe,
    Icon,
    PageHeader,
    SearchField,
    UiSelectField,
    Stack,
    StatusBadge,
    UiButton,
    UiDataTable,
    UiSortHeader,
  ],
})
export class ContainersList {
  private readonly router = inject(Router);
  private readonly containerTypes = inject(ContainerTypeOptionsService);
  protected readonly store = inject(ContainersListStore);
  protected readonly Permission = Permission;

  protected readonly statusOptions = containerStatusOptions;
  protected readonly containerTypeOptions = this.containerTypes.options;

  protected readonly statusFilter = signal<ContainerStatus | null>(null);
  protected readonly containerTypeFilter = signal<string | null>(null);

  constructor() {
    void this.containerTypes.load();
  }

  protected search(value: string): void {
    this.store.setSearch(value);
  }

  protected onStatusChange(status: ContainerStatus | null): void {
    this.statusFilter.set(status);
    this.applyFilters();
  }

  protected onContainerTypeChange(containerTypeId: string | null): void {
    this.containerTypeFilter.set(containerTypeId);
    this.applyFilters();
  }

  protected addContainer(): void {
    this.router.navigate(["/containers/add"]);
  }

  protected viewContainer(container: ContainerDto): void {
    if (container.id) {
      this.router.navigate(["/containers", container.id]);
    }
  }

  protected editContainer(container: ContainerDto): void {
    if (container.id) {
      this.router.navigate(["/containers", container.id, "edit"]);
    }
  }

  private applyFilters(): void {
    const filters: Record<string, unknown> = {};
    const status = this.statusFilter();
    const containerTypeId = this.containerTypeFilter();
    if (status) filters["Status"] = status;
    if (containerTypeId) filters["ContainerTypeId"] = containerTypeId;
    this.store.setFilters(filters);
  }
}
