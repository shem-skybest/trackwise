import { Permission } from "@trackwise/shared/models/permissions";
import type { NavSection } from "@/shared/layout/nav-menu";

export const forwardingNav: NavSection = {
  id: "forwarding",
  label: "Forwarding",
  items: [
    {
      id: "Tariff & Cost Matrix",
      label: "Tariff & Cost Matrix",
      icon: "calculator",
      route: "/tariffs",
      permission: Permission.Tariff.View,
    },
    {
      id: "Quotes & Inquiries",
      label: "Quotes & Inquiries",
      icon: "file-text",
      route: "/quotations",
      permission: Permission.Quotation.View,
    },
    {
      id: "Bookings & Executions",
      label: "Bookings & Executions",
      icon: "calendar",
      route: "/bookings-executions",
      permission: Permission.Booking.Manage,
    },
    // {
    //   id: "Workfiles",
    //   label: "Workfiles",
    //   icon: "folder",
    //   route: "/workfiles",
    //   permission: Permission.Workfile.View,
    // },
    // {
    //   id: "Compliance Queue",
    //   label: "Compliance Queue",
    //   icon: "check",
    //   route: "/forwarding/compliance-queue",
    //   permission: Permission.Compliance.View,
    //   comingSoon: true,
    // },
    {
      id: "Billing & Invoicing",
      label: "Billing & Invoicing",
      icon: "file",
      route: "/billing-invoicing",
      permission: Permission.Billing.View,
      // comingSoon: true,
    },
    {
      id: "Shipment Tracking",
      label: "Shipment Tracking",
      icon: "truck",
      route: "/shipment-tracking",
      permission: Permission.Shipments.View,
      comingSoon: true,
    },
    {
      id: "Audit Logs",
      label: "Audit Logs",
      icon: "history",
      route: "/audit-logs",
      permission: Permission.Billing.View,
      comingSoon: true,
    },
  ],
};
