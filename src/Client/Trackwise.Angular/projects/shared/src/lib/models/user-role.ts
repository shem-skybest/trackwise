import type { SelectOption } from "./select-option";

/**
 * Mirrors `TenantRoles` / `AppRoles` in C#. Access is decided by permissions, not by these names -
 * they exist for labels and the few places that genuinely care which role it is (drivers, owner).
 * Roles created in Settings have keys that start with `tenant.custom_` and aren't listed here.
 */
export enum UserRole {
  AppSuperAdmin = "app.superadmin",
  AppAdmin = "app.admin",
  Owner = "tenant.owner",
  Executive = "tenant.executive",
  SystemAdmin = "tenant.system_admin",
  Manager = "tenant.manager",
  ColdChainManager = "tenant.cold_chain_manager",
  ForwardingSupervisor = "tenant.forwarding_supervisor",
  Dispatcher = "tenant.dispatcher",
  LiaisonOfficer = "tenant.liaison_officer",
  FleetSupervisor = "tenant.fleet_supervisor",
  Marketing = "tenant.marketing",
  MarketingManager = "tenant.marketing_manager",
  Accounting = "tenant.accounting",
  AccountingManager = "tenant.accounting_manager",
  HumanResources = "tenant.hr",
  AdminStaff = "tenant.admin_staff",
  Driver = "tenant.driver",
  Customer = "tenant.customer",
}

export const userRoleOptions: SelectOption<UserRole>[] = [
  { label: "Super Admin", value: UserRole.AppSuperAdmin },
  { label: "Admin", value: UserRole.AppAdmin },
  { label: "Owner", value: UserRole.Owner },
  { label: "Executive", value: UserRole.Executive },
  { label: "System Admin (IT)", value: UserRole.SystemAdmin },
  { label: "Ops Manager", value: UserRole.Manager },
  { label: "Cold Chain Manager", value: UserRole.ColdChainManager },
  { label: "Forwarding Supervisor", value: UserRole.ForwardingSupervisor },
  { label: "CS + Dispatch", value: UserRole.Dispatcher },
  { label: "Liaison Officer", value: UserRole.LiaisonOfficer },
  { label: "Fleet Supervisor", value: UserRole.FleetSupervisor },
  { label: "Marketing", value: UserRole.Marketing },
  { label: "Marketing Manager", value: UserRole.MarketingManager },
  { label: "Accounting", value: UserRole.Accounting },
  { label: "Accounting Manager", value: UserRole.AccountingManager },
  { label: "HR", value: UserRole.HumanResources },
  { label: "Admin Staff", value: UserRole.AdminStaff },
  { label: "Driver / Helper", value: UserRole.Driver },
  { label: "Customer", value: UserRole.Customer },
] as const;

/** Any staff role of a company - built-in or created in Settings - as opposed to platform or portal roles. */
export function isTenantStaffRole(role: string | null | undefined): boolean {
  return !!role && role.startsWith("tenant.") && role !== UserRole.Customer;
}
