import { Injectable } from "@angular/core";
import { getAccessToken } from "@trackwise/shared";
import type { AgentDecisionDto, AgentMessageDto } from "@trackwise/shared/api";
import type { AgentTurnUpdate } from "./agent-chat.contracts";
import { BaseHubConnection } from "./base-hub-connection";

/**
 * Real-time copilot events. The hub is authorized: identity comes from the JWT and the server
 * auto-joins the connection to its private per-user group - no RegisterTenant handshake.
 *
 * CopilotStore is the only intended subscriber - components read the store, never this service.
 */
@Injectable({ providedIn: "root" })
export class CopilotHubService extends BaseHubConnection {
  readonly messageReceived$ = this.event<AgentMessageDto>("ReceiveCopilotMessage");
  readonly decisionReceived$ = this.event<AgentDecisionDto>("ReceiveCopilotDecision");
  readonly turnUpdateReceived$ = this.event<AgentTurnUpdate>("ReceiveCopilotTurnUpdate");

  constructor() {
    super("copilot", {
      accessTokenFactory: () => getAccessToken("tmsportal") ?? "",
      registerTenant: false,
    });
  }
}
