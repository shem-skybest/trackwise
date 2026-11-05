using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Agents.Services;

internal interface IAgentDecisionExecution : IApplicationService
{
    /// <summary>
    /// Runs an approved decision's tool and records the outcome on it, then hands the transcript
    /// note to <paramref name="appendNoteAsync"/>. A tool failure marks the decision Failed and is
    /// reported through the returned result - the note is written either way, so the caller must
    /// not skip it on failure.
    /// </summary>
    Task<Result> ExecuteAndNoteAsync(
        AgentDecision decision, Func<string, Task> appendNoteAsync, CancellationToken ct);
}
