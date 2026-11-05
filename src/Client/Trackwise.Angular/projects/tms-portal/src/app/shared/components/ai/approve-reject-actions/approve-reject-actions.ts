import { Component, input, output } from "@angular/core";
import type { AgentDecisionDto } from "@trackwise/shared/api";
import { UiButton } from "@trackwise/shared/ui";

@Component({
  selector: "app-approve-reject-actions",
  templateUrl: "./approve-reject-actions.html",
  imports: [UiButton],
})
export class ApproveRejectActions {
  public readonly decision = input.required<AgentDecisionDto>();
  /** True while this decision's approve/reject request is in flight. */
  public readonly busy = input(false);
  public readonly approve = output<AgentDecisionDto>();
  public readonly reject = output<AgentDecisionDto>();
}
