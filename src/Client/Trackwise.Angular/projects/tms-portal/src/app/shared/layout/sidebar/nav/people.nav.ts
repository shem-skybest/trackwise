import { Permission } from "@trackwise/shared";
import type { NavSection } from "@/shared/layout/nav-menu";

export const peopleNav: NavSection = {
  id: "people",
  label: "People & Partners",
  items: [
    {
      id: "customers",
      label: "Customers",
      icon: "building-2",
      route: "/customers",
      feature: "customers",
      permission: Permission.Customer.View,
    },
  ],
};
