import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import {
  Api,
  deleteEmployeeMasterData,
  getDocuments,
  getEmployeeById,
  getLoads,
  type DocumentDto,
  type EmployeeDto as EmployeeMasterDataDto,
  type LoadDto,
} from "@trackwise/shared/api";

/**
 * The API's answer when the dependency probe or the live-login guard stops a delete - see
 * `DeleteEmployeeHandler` for why removing an employee needs an explicit confirmation beyond the
 * usual dependency-in-use case. Shown verbatim by the component rather than replaced generically.
 */
const RECORD_IN_USE = "MASTER_DATA_RECORD_IN_USE";

interface EmployeeDetailsState {
  employeeId: string | null;
  employee: EmployeeMasterDataDto | null;
  recentLoads: LoadDto[];
  documents: DocumentDto[];
  isLoading: boolean;
  isProcessing: boolean;
  error: string | null;
  editDialogVisible: boolean;
}

const initialState: EmployeeDetailsState = {
  employeeId: null,
  employee: null,
  recentLoads: [],
  documents: [],
  isLoading: false,
  isProcessing: false,
  error: null,
  editDialogVisible: false,
};

export const EmployeeDetailsStore = signalStore(
  withState(initialState),
  withComputed((store) => {
    const fullName = computed(() => store.employee()?.fullName ?? "");
    const hasRole = computed(() => !!store.employee()?.role);
    const isActive = computed(() => store.employee()?.status === "active");
    const recentLoadsCount = computed(() => store.recentLoads().length);
    const documentsCount = computed(() => store.documents().length);

    return {
      fullName,
      hasRole,
      isActive,
      recentLoadsCount,
      documentsCount,
    };
  }),
  withMethods((store, api = inject(Api)) => ({
    reset() {
      patchState(store, initialState);
    },

    async loadEmployee(employeeId: string) {
      patchState(store, { employeeId, isLoading: true, error: null });

      try {
        const [employee, loadsResponse, documentsResponse] = await Promise.all([
          api.invoke(getEmployeeById, { userId: employeeId }),
          api.invoke(getLoads, {
            UserId: employeeId,
            PageSize: 10,
            OrderBy: "-DeliveryDate",
          }),
          api.invoke(getDocuments, {
            OwnerId: employeeId,
            OwnerType: "employee",
          }),
        ]);

        patchState(store, {
          employee,
          recentLoads: loadsResponse?.items ?? [],
          documents: documentsResponse ?? [],
          isLoading: false,
        });
      } catch (error) {
        patchState(store, {
          error: "Failed to load employee details",
          isLoading: false,
        });
        console.error("Failed to load employee:", error);
      }
    },

    async refreshEmployee() {
      const employeeId = store.employeeId();
      if (!employeeId) return;

      try {
        const employee = await api.invoke(getEmployeeById, { userId: employeeId });
        patchState(store, { employee });
      } catch (error) {
        console.error("Failed to refresh employee:", error);
      }
    },

    async refreshLoads() {
      const employeeId = store.employeeId();
      if (!employeeId) return;

      try {
        const loadsResponse = await api.invoke(getLoads, {
          UserId: employeeId,
          PageSize: 10,
          OrderBy: "-DeliveryDate",
        });
        patchState(store, { recentLoads: loadsResponse?.items ?? [] });
      } catch (error) {
        console.error("Failed to refresh loads:", error);
      }
    },

    async refreshDocuments() {
      const employeeId = store.employeeId();
      if (!employeeId) return;

      try {
        const documentsResponse = await api.invoke(getDocuments, {
          OwnerId: employeeId,
          OwnerType: "employee",
        });
        patchState(store, { documents: documentsResponse ?? [] });
      } catch (error) {
        console.error("Failed to refresh documents:", error);
      }
    },

    /**
     * Soft-deletes the employee. Returns the conflict message verbatim when the dependency probe
     * or the live-login guard refuses the write (`RECORD_IN_USE`), so the component can show it
     * instead of a generic failure - same pattern as `truck-edit.ts`'s `conflictMessage`.
     */
    async deleteEmployee(): Promise<{ success: boolean; message?: string }> {
      const employeeId = store.employeeId();
      if (!employeeId) return { success: false };

      patchState(store, { isProcessing: true, error: null });

      try {
        await api.invoke(deleteEmployeeMasterData, { id: employeeId });
        patchState(store, { isProcessing: false });
        return { success: true };
      } catch (error) {
        const body = (error as { error?: { errorCode?: string; message?: string } })?.error;
        const message = body?.errorCode === RECORD_IN_USE ? (body.message ?? undefined) : undefined;

        patchState(store, {
          error: message ?? "Failed to delete employee",
          isProcessing: false,
        });
        return { success: false, message };
      }
    },

    openEditDialog() {
      patchState(store, { editDialogVisible: true });
    },

    closeEditDialog() {
      patchState(store, { editDialogVisible: false });
    },
  })),
);
