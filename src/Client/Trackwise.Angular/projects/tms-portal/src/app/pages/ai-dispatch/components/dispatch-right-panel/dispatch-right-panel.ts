import { Component, inject } from "@angular/core";
import { Permission } from "@trackwise/shared";
import type { AgentDecisionDto } from "@trackwise/shared/api";
import { CountBadge, EmptyState, Icon, Stack, Surface, Typography } from "@trackwise/shared/ui";
import { AgentDecisionCard, AIQuotaUsage, DecisionActionsService } from "@/shared/components";
import { DispatchChatStore } from "../../store/dispatch-chat.store";
import { FleetMapCard } from "../fleet-map-card/fleet-map-card";

/**
 * Right panel of the dispatch chat page: fleet map, tenant-wide pending write decisions, and the
 * AI quota meter. Collapsible - the page header owns the toggle button, the store owns the state.
 */
@Component({
  selector: "app-dispatch-right-panel",
  templateUrl: "./dispatch-right-panel.html",
  imports: [
    AgentDecisionCard,
    AIQuotaUsage,
    CountBadge,
    EmptyState,
    FleetMapCard,
    Icon,
    Stack,
    Surface,
    Typography,
  ],
})
export class DispatchRightPanel {
  protected readonly store = inject(DispatchChatStore);
  protected readonly actions = inject(DecisionActionsService);

  protected readonly dispatchManage = Permission.Dispatch.Manage;

  protected approve(decision: AgentDecisionDto): void {
    this.actions.approve(decision, () => this.store.onDecisionResolved());
  }

  protected reject(decision: AgentDecisionDto): void {
    this.actions.reject(decision, () => this.store.onDecisionResolved());
  }
}
