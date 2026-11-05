import { Permission } from "@trackwise/shared";
import type { NavSection } from "@/shared/layout/nav-menu";

export const fleetNav: NavSection = {
  id: "fleet",
  label: "Fleet & Compliance",
  items: [
    {
      id: "eld",
      label: "ELD / HOS",
      icon: "clock",
      route: "/eld",
      feature: "eld",
      permission: Permission.Eld.View,
    },
    {
      id: "maintenance",
      label: "Maintenance",
      icon: "wrench",
      feature: "maintenance",
      permission: Permission.Maintenance.View,
      children: [
        {
          id: "maintenance-dashboard",
          label: "Dashboard",
          route: "/maintenance",
          permission: Permission.Maintenance.View,
        },
        {
          id: "maintenance-records",
          label: "Service Records",
          route: "/maintenance/records",
          permission: Permission.Maintenance.View,
        },
        {
          id: "maintenance-upcoming",
          label: "Upcoming Service",
          route: "/maintenance/upcoming",
          permission: Permission.Maintenance.View,
        },
      ],
    },
    {
      id: "vehicle-papers",
      label: "Vehicle Papers",
      icon: "file-check",
      route: "/vehicle-papers",
      permission: Permission.Truck.View,
    },
    {
      id: "incidents",
      label: "Incidents",
      icon: "triangle-alert",
      route: "/incidents",
      permission: Permission.Safety.View,
    },
    {
      id: "dvir",
      label: "DVIR Reports",
      icon: "clipboard",
      route: "/safety/dvir",
      feature: "dvir",
      permission: Permission.Dvir.View,
    },
    {
      id: "safety",
      label: "Safety",
      icon: "shield",
      feature: "safety",
      permission: Permission.Safety.View,
      children: [
        {
          id: "safety-overview",
          label: "Overview",
          route: "/safety",
          permission: Permission.Safety.View,
        },
        {
          id: "safety-driver-behavior",
          label: "Driver Behavior",
          route: "/safety/driver-behavior",
          permission: Permission.Safety.View,
        },
        {
          id: "safety-condition-reports",
          label: "Condition Reports",
          route: "/safety/condition-reports",
          permission: Permission.Safety.View,
        },
      ],
    },
  ],
};
