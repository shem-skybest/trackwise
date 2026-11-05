using Trackwise.Application.Abstractions.AI;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.AI.Agents;

/// <summary>
/// What distinguishes one conversational agent from another inside
/// <see cref="AgentTurnService"/>: prompt/tool setup and where broadcasts go.
/// </summary>
internal interface IAgentSurface
{
    /// <summary>Stamped on the <see cref="AgentSession"/> the turn service creates.</summary>
    AgentSessionType SessionType { get; }

    /// <summary>
    /// Resolves the LLM conversation and tool-call context for one turn. Called once, before the
    /// agent loop runs.
    /// </summary>
    Task<AgentTurnSetup> PrepareAsync(
        AgentSession session,
        AgentConversation conversation,
        AgentTurnRequest request,
        LlmOptions config,
        CancellationToken ct);

    /// <summary>Pushes one transcript row to the conversation's listeners.</summary>
    Task BroadcastMessageAsync(AgentTurnRequest request, AgentConversation conversation, AgentMessageDto message);

    /// <summary>Pushes turn progress (status, tokens, decisions) to the conversation's listeners.</summary>
    Task BroadcastTurnUpdateAsync(AgentTurnRequest request, AgentConversation conversation, AgentSession session);
}
