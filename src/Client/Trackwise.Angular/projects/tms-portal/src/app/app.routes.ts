import { isDevMode } from "@angular/core";
import type { Routes } from "@angular/router";
import { featureGuardFromData } from "@trackwise/shared";
import { SignInRedirect } from "@trackwise/shared/auth";
import { NotFoundComponent, UnauthorizedComponent } from "@/pages/errors";

export const appRoutes: Routes = [
  {
    path: "home",
    loadChildren: () => import("./pages/home/home.routes").then((m) => m.homeRoutes),
  },
  {
    path: "reports",
    loadChildren: () => import("./pages/reports/reports.routes").then((m) => m.reportsRoutes),
    canActivate: [featureGuardFromData],
    data: { breadcrumb: "Reports", feature: "reports" },
  },
  {
    // /dashboard merged into /home (role-aware panel registry); kept as redirect for bookmarks.
    path: "dashboard",
    loadChildren: () => import("./pages/dashboard/dashboard.routes").then((m) => m.dashboardRoutes),
  },
  {
    // Writes go through Permission.MasterData.Manage (see employee.routes.ts); not feature-gated,
    // matching Truck's promotion - an employee record is reference data HR maintains whether or
    // not the tenant has the trucking-dispatch feature turned on.
    path: "master-data/employees",
    loadChildren: () => import("./pages/employees/employee.routes").then((m) => m.employeeRoutes),
    data: {
      breadcrumb: "Employees",
    },
  },
  // Forwarding (TMS) section. Each child route is guarded by `featureGuardFromData` to enforce
  // the Starter-tier feature flag. Each child route is also guarded by `authGuard` to enforce
  // role and route-permission checks.
  {
    path: "tariffs",
    loadChildren: () =>
      import("./pages/forwarding/tariff/tariff.routes").then((m) => m.TariffRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Tariffs",
      feature: "tariffs",
    },
  },
  {
    path: "quotations",
    loadChildren: () =>
      import("./pages/forwarding/quotation/quotation.routes").then((m) => m.QuotationRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Quotations",
      feature: "quotations",
    },
  },
  {
    path: "billing-invoicing",
    loadChildren: () =>
      import("./pages/forwarding/billing/billing.routes").then((m) => m.BillingRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Billing & Invoicing",
      feature: "billing-invoicing",
    },
  },
  {
    path: "workfiles",
    loadChildren: () =>
      import("./pages/forwarding/workfile/workfile.routes").then((m) => m.WorkfileRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Workfiles",
      feature: "workfiles",
    },
  },
  {
    path: "master-data/branches",
    loadChildren: () =>
      import("./pages/master-data/branch/branch.routes").then((m) => m.BranchRoutes),
    data: {
      breadcrumb: "Branches",
    },
  },
  {
    path: "master-data/ports",
    loadChildren: () => import("./pages/master-data/port/port.routes").then((m) => m.PortRoutes),
    data: {
      breadcrumb: "Ports",
    },
  },
  {
    path: "master-data/locations",
    loadChildren: () =>
      import("./pages/master-data/location/location.routes").then((m) => m.LocationRoutes),
    data: {
      breadcrumb: "Locations",
    },
  },
  {
    path: "master-data/vessels",
    loadChildren: () =>
      import("./pages/master-data/vessel/vessel.routes").then((m) => m.VesselRoutes),
    data: {
      breadcrumb: "Vessels",
    },
  },
  {
    path: "master-data/commodities",
    loadChildren: () =>
      import("./pages/master-data/commodity/commodity.routes").then((m) => m.CommodityRoutes),
    data: {
      breadcrumb: "Commodities",
    },
  },
  {
    path: "master-data/container-types",
    loadChildren: () =>
      import("./pages/master-data/container-type/container-type.routes").then(
        (m) => m.ContainerTypeRoutes,
      ),
    data: {
      breadcrumb: "Container Types",
    },
  },
  {
    path: "master-data/currencies",
    loadChildren: () =>
      import("./pages/master-data/currency/currency.routes").then((m) => m.CurrencyRoutes),
    data: {
      breadcrumb: "Currencies",
    },
  },
  {
    // Not feature-gated, unlike the /business-partners route it replaces. A partner is reference
    // data marketing and accounting maintain whether or not the tenant has the workfiles feature,
    // and the master-data routes gate on Permission.MasterData.* alone - the same call ports made.
    path: "master-data/partners",
    loadChildren: () =>
      import("./pages/master-data/partner/partner.routes").then((m) => m.PartnerRoutes),
    data: {
      breadcrumb: "Business Partners",
    },
  },
  {
    path: "master-data/trailers",
    loadChildren: () =>
      import("./pages/master-data/trailer/trailer.routes").then((m) => m.TrailerRoutes),
    data: {
      breadcrumb: "Trailers",
    },
  },
  {
    path: "master-data/helpers",
    loadChildren: () =>
      import("./pages/master-data/helper/helper.routes").then((m) => m.HelperRoutes),
    data: {
      breadcrumb: "Helpers",
    },
  },
  {
    path: "master-data/gensets",
    loadChildren: () =>
      import("./pages/master-data/genset/genset.routes").then((m) => m.GensetRoutes),
    data: {
      breadcrumb: "Gensets",
    },
  },
  {
    path: "master-data/approvals",
    loadChildren: () =>
      import("./pages/master-data/approvals/approvals.routes").then((m) => m.ApprovalsRoutes),
    data: {
      breadcrumb: "Approvals",
    },
  },
  {
    path: "bookings-executions",
    loadChildren: () =>
      import("./pages/forwarding/booking/booking.routes").then((m) => m.BookingRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Bookings & Executions",
      feature: "bookings",
    },
  },
  {
    path: "loads",
    loadChildren: () => import("./pages/loads/load.routes").then((m) => m.loadRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Loads",
      feature: "loads",
    },
  },
  {
    path: "containers",
    loadChildren: () =>
      import("./pages/containers/container.routes").then((m) => m.containerRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Containers",
      feature: "intermodal_containers",
    },
  },
  {
    // Writes go through Permission.MasterData.Manage (see truck.routes.ts); not feature-gated,
    // matching every other master-data route - a truck is reference data a steward maintains
    // whether or not the tenant has the trucking-dispatch feature turned on.
    path: "master-data/trucks",
    loadChildren: () => import("./pages/trucks/truck.routes").then((m) => m.truckRoutes),
    data: {
      breadcrumb: "Trucks",
    },
  },
  {
    path: "customers",
    loadChildren: () => import("./pages/customers/customer.routes").then((m) => m.customerRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Customers",
      feature: "customers",
    },
  },
  {
    path: "payment",
    loadChildren: () => import("./pages/payment/payment.routes").then((m) => m.paymentRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "",
      feature: "payments",
    },
  },
  {
    path: "invoices",
    loadChildren: () => import("./pages/invoices/invoice.routes").then((m) => m.invoiceRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "",
      feature: "invoices",
    },
  },
  {
    path: "settings",
    loadChildren: () => import("./pages/settings/settings.routes").then((m) => m.settingsRoutes),
    data: {
      breadcrumb: "Settings",
    },
  },
  {
    path: "subscription",
    loadChildren: () =>
      import("./pages/subscription/subscription.routes").then((m) => m.subscriptionRoutes),
  },
  {
    path: "ai-dispatch",
    loadChildren: () =>
      import("./pages/ai-dispatch/ai-dispatch.routes").then((m) => m.aiDispatchRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "AI Dispatch",
      feature: "agentic_dispatch",
    },
  },
  {
    path: "rate-agreements",
    loadChildren: () =>
      import("./pages/rate-agreements/rate-agreement.routes").then((m) => m.RateAgreementRoutes),
    data: { breadcrumb: "Rate Agreements" },
  },
  {
    path: "trips",
    loadChildren: () => import("./pages/trips/trip.routes").then((m) => m.tripRoutes),
    canActivate: [featureGuardFromData],
    data: { feature: "trips" },
  },
  {
    path: "crew-plan",
    loadChildren: () => import("./pages/crew-plan/crew-plan.routes").then((m) => m.crewPlanRoutes),
    canActivate: [featureGuardFromData],
    data: { breadcrumb: "Crew Plan", feature: "trips" },
  },
  {
    path: "eld",
    loadChildren: () => import("./pages/eld/eld.routes").then((m) => m.eldRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "ELD / HOS",
      feature: "eld",
    },
  },
  {
    path: "loadboard",
    loadChildren: () => import("./pages/loadboard/loadboard.routes").then((m) => m.loadBoardRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Load Board",
      feature: "load_board",
    },
  },
  {
    path: "fuel-cards",
    loadChildren: () =>
      import("./pages/fuel-cards/fuel-cards.routes").then((m) => m.fuelCardsRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Fuel Cards",
      feature: "fuel_cards",
    },
  },
  {
    path: "messages",
    loadChildren: () => import("./pages/messages/messages.routes").then((m) => m.messagesRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Messages",
      feature: "messages",
    },
  },
  {
    path: "notifications",
    loadChildren: () =>
      import("./pages/notifications/notifications.routes").then((m) => m.notificationsRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Notifications",
      feature: "notifications",
    },
  },
  {
    path: "safety",
    loadChildren: () => import("./pages/safety/safety.routes").then((m) => m.inspectionRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Safety",
      // DVIR is its own Starter-tier feature; each child re-declares which of the two admits it.
      feature: ["safety", "dvir"],
    },
  },
  {
    path: "expenses",
    loadChildren: () => import("./pages/expenses/expense.routes").then((m) => m.expenseRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Expenses",
      feature: "expenses",
    },
  },
  {
    path: "billing-batches",
    loadChildren: () =>
      import("./pages/billing-batches/billing-batch.routes").then((m) => m.billingBatchRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Billing Batches",
      feature: "invoices",
    },
  },
  {
    path: "incidents",
    loadChildren: () => import("./pages/incidents/incident.routes").then((m) => m.incidentRoutes),
    data: { breadcrumb: "Incidents" },
  },
  {
    path: "cash-funds",
    loadChildren: () => import("./pages/cash-funds/cash-fund.routes").then((m) => m.cashFundRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Cash Funds",
      feature: "expenses",
    },
  },
  {
    path: "fuel-log",
    loadChildren: () => import("./pages/fuel-log/fuel-log.routes").then((m) => m.fuelLogRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Fuel Log",
      feature: "expenses",
    },
  },
  {
    path: "payroll",
    loadChildren: () => import("./pages/payroll/payroll.routes").then((m) => m.payrollRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Payroll",
      feature: "payroll",
    },
  },
  {
    path: "timesheets",
    loadChildren: () =>
      import("./pages/timesheets/timesheets.routes").then((m) => m.timesheetsRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Timesheets",
      feature: "timesheets",
    },
  },
  {
    path: "vehicle-papers",
    loadChildren: () =>
      import("./pages/vehicle-papers/vehicle-papers.routes").then((m) => m.vehiclePapersRoutes),
    data: { breadcrumb: "Vehicle Papers" },
  },
  {
    path: "maintenance",
    loadChildren: () =>
      import("./pages/maintenance/maintenance.routes").then((m) => m.maintenanceRoutes),
    canActivate: [featureGuardFromData],
    data: {
      breadcrumb: "Maintenance",
      feature: "maintenance",
    },
  },
  {
    // Dev-only component gallery. `canMatch` (not `canActivate`) so a production build never even
    // resolves the route - it falls through to the `**` redirect. No auth guard and no feature
    // guard on purpose: the lab renders from local fixtures and must work signed-out.
    path: "ui-lab",
    canMatch: [() => isDevMode()],
    loadComponent: () => import("./pages/ui-lab/ui-lab").then((m) => m.UiLab),
  },
  {
    path: "",
    component: SignInRedirect,
    data: { homeUrl: "/home" },
  },
  {
    path: "unauthorized",
    component: UnauthorizedComponent,
  },
  {
    path: "404",
    component: NotFoundComponent,
  },
  {
    path: "**",
    redirectTo: "404",
  },
];
