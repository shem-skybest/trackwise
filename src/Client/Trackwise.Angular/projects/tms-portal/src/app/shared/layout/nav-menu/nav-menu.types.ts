import type { TenantFeature } from "@trackwise/shared/api";
import type { IconName } from "@trackwise/shared/ui";

export interface NavSection {
  id: string;
  label: string;
  items: NavItem[];
  pinToBottom?: boolean;
}

export interface NavItem {
  id: string;
  label: string;
  /** Canonical `<ui-icon name>`. Typed, so a bad name is a compile error, not a blank glyph. */
  icon?: IconName;
  route?: string;
  /** One flag, or several of which **any** enabled one shows the item. */
  feature?: TenantFeature | TenantFeature[];
  /** Mirrors the route's `data.permission`, so the menu never renders a link that denies on click. */
  permission?: string;
  children?: NavItem[];
  badge?: () => number | null;
  /**
   * Kept in the data (so it stays resolvable for the command palette and stored favorites) but
   * hidden from the rendered menus. Stripped from `menuSections`, retained in `fullSections`.
   */
  menuHidden?: boolean;
  /**
   * The route it points to has no page behind it yet. Renders greyed-out and inert instead of a
   * live link, and is excluded from the command palette index and the favorites bar.
   */
  comingSoon?: boolean;
}
