import type { NavSection } from "@/shared/layout/nav-menu";
import { dispatchNav } from "./nav/dispatch.nav";
import { financeNav } from "./nav/finance.nav";
import { fleetNav } from "./nav/fleet.nav";
import { forwardingNav } from "./nav/forwarding.nav";
import { mainNav } from "./nav/main.nav";
import { masterDataNav } from "./nav/master-data.nav";
import { peopleNav } from "./nav/people.nav";
import { systemNav } from "./nav/system.nav";

// Array order = rendered order (top to bottom in the sidebar); `pinToBottom` sections
// (e.g. system) are pulled out separately by `NavMenu`, but the source order still matters
// for the command palette + favorites index.
export const sidebarSections: NavSection[] = [
  mainNav,
  forwardingNav,
  dispatchNav,
  fleetNav,
  financeNav,
  peopleNav,
  masterDataNav,
  systemNav,
];
