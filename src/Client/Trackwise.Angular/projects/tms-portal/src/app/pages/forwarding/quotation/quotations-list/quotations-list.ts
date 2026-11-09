import { Component, computed, effect, inject, signal } from "@angular/core";
import { form, FormField } from "@angular/forms/signals";
import { Router } from "@angular/router";
import { createListStore, getBranches } from "@trackwise/shared";
import type { BranchDto, CustomerDto, FreightMode, QuotationStatus } from "@trackwise/shared/api";
import {
  Card,
  Grid,
  Stack,
  Typography,
  UiButton,
  UiFormField,
  UiSelectField,
  UiTextField,
} from "@trackwise/shared/ui";
import { PageHeader } from "@/shared/components";
import { SearchCustomer } from "@/shared/components/search";
import { QuotationsTable } from "../components/quotations-table/quotations-table";
import { QuotationsListStore } from "../store/quotations-list.store";

const ALL = "all";

const BranchesStore = createListStore<BranchDto>(getBranches, {
  defaultSortField: "Name",
  defaultPageSize: 100,
});

interface QuotationFilters {
  search: string;
  customer: CustomerDto | null;
  branchId: string;
  freightMode: string;
  status: string;
}

const DEFAULT_FILTERS: QuotationFilters = {
  search: "",
  customer: null,
  branchId: ALL,
  freightMode: ALL,
  status: ALL,
};

@Component({
  selector: "app-quotations-list",
  templateUrl: "./quotations-list.html",
  providers: [QuotationsListStore, BranchesStore],
  imports: [
    PageHeader,
    QuotationsTable,
    SearchCustomer,
    FormField,
    Card,
    Grid,
    Stack,
    Typography,
    UiButton,
    UiFormField,
    UiSelectField,
    UiTextField,
  ],
})
export class QuotationsListComponent {
  private readonly router = inject(Router);
  protected readonly store = inject(QuotationsListStore);
  protected readonly branchesStore = inject(BranchesStore);

  protected readonly filterModel = signal<QuotationFilters>({ ...DEFAULT_FILTERS });

  protected readonly filterForm = form(this.filterModel);

  protected readonly hasActiveFilters = computed(() => {
    const f = this.filterModel();
    return (
      f.search.trim() !== "" ||
      f.customer !== null ||
      f.branchId !== ALL ||
      f.freightMode !== ALL ||
      f.status !== ALL
    );
  });

  protected readonly activeFilterCount = computed(() => {
    const f = this.filterModel();
    return [
      f.search.trim() !== "",
      f.customer !== null,
      f.branchId !== ALL,
      f.freightMode !== ALL,
      f.status !== ALL,
    ].filter(Boolean).length;
  });

  protected clearFilters(): void {
    this.filterModel.set({ ...DEFAULT_FILTERS });
  }

  protected readonly branchOptions = computed(() => [
    { label: "All branches", value: ALL },
    ...this.branchesStore.data().map((b) => ({
      value: b.id ?? "",
      label: b.code ? `${b.code} — ${b.name}` : (b.name ?? ""),
    })),
  ]);

  protected readonly freightModeOptions: { label: string; value: FreightMode | typeof ALL }[] = [
    { label: "All freight modes", value: ALL },
    { label: "Domestic", value: "domestic" },
    { label: "Import", value: "import" },
    { label: "Export", value: "export" },
    { label: "Triangular", value: "triangular" },
  ];

  protected readonly statusOptions: { label: string; value: QuotationStatus | typeof ALL }[] = [
    { label: "All statuses", value: ALL },
    { label: "Draft", value: "draft" },
    { label: "Pending Approval", value: "pending_approval" },
    { label: "Approved", value: "approved" },
    { label: "Issued", value: "issued" },
    { label: "Accepted", value: "accepted" },
    { label: "Rejected", value: "rejected" },
    { label: "Auto Declined", value: "auto_declined" },
    { label: "Expired", value: "expired" },
  ];

  constructor() {
    this.branchesStore.setFilters({ IsActive: true }, { reload: true });

    let firstRun = true;
    effect((onCleanup) => {
      const f = this.filterModel();

      // The table's own initial lazy-load already fetches, so skip the first run
      if (firstRun) {
        firstRun = false;
        return;
      }

      // Debounce so typing in search doesn't fire a request per keystroke
      const timer = setTimeout(() => {
        const pick = (v: string) => (v === ALL || !v ? undefined : v);
        this.store.setFilters({
          Search: f.search.trim() || undefined,
          CustomerId: f.customer?.id || undefined,
          BranchId: pick(f.branchId),
          FreightMode: pick(f.freightMode),
          Status: pick(f.status),
        });
      }, 300);

      onCleanup(() => clearTimeout(timer));
    });
  }

  protected addQuotation(): void {
    this.router.navigate(["/quotations/add"]);
  }
}
