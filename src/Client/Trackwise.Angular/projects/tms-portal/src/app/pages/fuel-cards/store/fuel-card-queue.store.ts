import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import {
  Api,
  assignFuelCardTransactionTruck,
  getFuelCardTransactions,
  getTrucks,
  ignoreFuelCardTransaction,
  type FuelCardProviderType,
  type FuelCardTransactionDto,
  type FuelCardTransactionStatus,
  type TruckDto,
} from "@trackwise/shared/api";
import { ToastService } from "@/core/services";

export type FuelCardStatusFilter = FuelCardTransactionStatus | "all";

export interface AssignTruckSelection {
  truckId: string;
  rememberMapping: boolean;
}

interface FuelCardQueueState {
  transactions: FuelCardTransactionDto[];
  trucks: TruckDto[];
  loading: boolean;
  error: string | null;
  pendingCount: number;
  statusFilter: FuelCardStatusFilter;
  providerFilter: FuelCardProviderType | null;
  dateRange: Date[];
  search: string;
  selection: FuelCardTransactionDto[];
}

const initialState: FuelCardQueueState = {
  transactions: [],
  trucks: [],
  loading: true,
  error: null,
  pendingCount: 0,
  statusFilter: "pending",
  providerFilter: null,
  dateRange: [],
  search: "",
  selection: [],
};

/**
 * Owns the fuel-card review queue: server data, the filter set, the pending-count badge, the bulk
 * selection, and the assign / ignore mutations. Provided per-component so state resets on navigation.
 * The page component keeps only the assign-dialog view-model.
 */
export const FuelCardQueueStore = signalStore(
  withState(initialState),
  withComputed((store) => ({
    // Checkboxes and bulk actions only make sense for the pending review queue.
    showSelection: computed(() => store.statusFilter() === "pending"),
    statusOptions: computed(() => [
      {
        label: store.pendingCount() ? `Pending (${store.pendingCount()})` : "Pending",
        value: "pending",
      },
      { label: "Matched", value: "matched" },
      { label: "Ignored", value: "ignored" },
      { label: "All", value: "all" },
    ]),
    // Server results narrowed by the client-side merchant / unit / truck search box.
    visibleTransactions: computed(() => {
      const term = store.search().trim().toLowerCase();
      const rows = store.transactions();
      if (!term) {
        return rows;
      }
      return rows.filter((t) =>
        [t.merchantName, t.merchantCity, t.unitNumber, t.truckNumber].some((v) =>
          v?.toLowerCase().includes(term),
        ),
      );
    }),
  })),
  withMethods((store, api = inject(Api), toast = inject(ToastService)) => {
    async function loadTransactions(): Promise<void> {
      patchState(store, { loading: true, error: null, selection: [] });
      try {
        const [from, to] = store.dateRange();
        const status = store.statusFilter();
        const data = await api.invoke(getFuelCardTransactions, {
          Status: status === "all" ? undefined : status,
          ProviderType: store.providerFilter() ?? undefined,
          FromDate: from ? startOfDayIso(from) : undefined,
          ToDate: to ? endOfDayIso(to) : undefined,
          PageSize: 200,
        });
        patchState(store, { transactions: data?.value ?? [] });
      } catch (err) {
        console.error("Error loading transactions:", err);
        patchState(store, { error: "Failed to load fuel card transactions" });
      } finally {
        patchState(store, { loading: false });
      }
    }

    async function refreshPendingCount(): Promise<void> {
      try {
        const data = await api.invoke(getFuelCardTransactions, { Status: "pending", PageSize: 1 });
        patchState(store, { pendingCount: data?.totalItems ?? 0 });
      } catch {
        // Non-critical - the badge keeps its previous value.
      }
    }

    async function loadTrucks(): Promise<void> {
      try {
        const data = await api.invoke(getTrucks, {});
        patchState(store, { trucks: data?.items ?? [] });
      } catch (err) {
        console.error("Error loading trucks:", err);
      }
    }

    async function reload(): Promise<void> {
      await Promise.all([loadTransactions(), refreshPendingCount()]);
    }

    return {
      loadTransactions,

      async init(status: string | null): Promise<void> {
        if (status && ["pending", "matched", "ignored", "all"].includes(status)) {
          patchState(store, { statusFilter: status as FuelCardStatusFilter });
        }
        await Promise.all([loadTransactions(), loadTrucks(), refreshPendingCount()]);
      },

      setStatus(statusFilter: FuelCardStatusFilter): void {
        patchState(store, { statusFilter });
        void loadTransactions();
      },

      setProvider(providerFilter: FuelCardProviderType | null): void {
        patchState(store, { providerFilter });
        void loadTransactions();
      },

      setDateRange(dateRange: Date[]): void {
        patchState(store, { dateRange });
        void loadTransactions();
      },

      setSearch(search: string): void {
        patchState(store, { search });
      },

      setSelection(selection: FuelCardTransactionDto[]): void {
        patchState(store, { selection: selection ?? [] });
      },

      /** Detail line for a single transaction, or "N transactions selected" for a bulk assign. */
      buildAssignSummary(targets: FuelCardTransactionDto[]): string {
        if (targets.length === 1) {
          return describeTransaction(targets[0]);
        }
        return `${targets.length} ${plural(targets.length, "transaction")} selected`;
      },

      /** Truck whose number matches a single transaction's unit number; null for bulk / no match. */
      suggestTruckId(targets: FuelCardTransactionDto[]): string | null {
        if (targets.length !== 1) {
          return null;
        }
        const unit = targets[0].unitNumber?.trim().toUpperCase();
        if (!unit) {
          return null;
        }
        return store.trucks().find((t) => t.number?.trim().toUpperCase() === unit)?.id ?? null;
      },

      async assign(targets: FuelCardTransactionDto[], req: AssignTruckSelection): Promise<void> {
        if (!targets.length) {
          return;
        }
        const results = await Promise.allSettled(
          targets.map((t) =>
            api.invoke(assignFuelCardTransactionTruck, {
              transactionId: t.id!,
              body: {
                transactionId: t.id!,
                truckId: req.truckId,
                rememberMapping: req.rememberMapping,
              },
            }),
          ),
        );

        const ok = results.filter((r) => r.status === "fulfilled").length;
        const failed = results.length - ok;
        if (ok) {
          toast.showSuccess(
            `Assigned ${ok} ${plural(ok, "transaction")}. Paid fuel ${plural(ok, "expense")} created.`,
          );
        }
        if (failed) {
          toast.showError(`Failed to assign ${failed} ${plural(failed, "transaction")}.`);
        }
        await reload();
      },

      async ignore(targets: FuelCardTransactionDto[]): Promise<void> {
        if (!targets.length) {
          return;
        }
        const results = await Promise.allSettled(
          targets.map((t) => api.invoke(ignoreFuelCardTransaction, { transactionId: t.id! })),
        );
        const failed = results.filter((r) => r.status === "rejected").length;
        if (failed) {
          toast.showError(`Failed to ignore ${failed} ${plural(failed, "transaction")}.`);
        }
        await reload();
      },
    };
  }),
);

function describeTransaction(transaction: FuelCardTransactionDto): string {
  const bits: string[] = [transaction.merchantName || "Unknown merchant"];
  if (transaction.merchantCity) {
    bits.push(transaction.merchantCity);
  }
  if (transaction.unitNumber) {
    bits.push(`Unit "${transaction.unitNumber}"`);
  }
  return bits.join(" · ");
}

function plural(count: number, word: string): string {
  return count === 1 ? word : `${word}s`;
}

function startOfDayIso(date: Date): string {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  return d.toISOString();
}

function endOfDayIso(date: Date): string {
  const d = new Date(date);
  d.setHours(23, 59, 59, 999);
  return d.toISOString();
}
