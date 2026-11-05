using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

/// <summary>
/// CreatedById is audit-only here - dispatch conversations are tenant-shared, so every user with
/// Dispatch.View can read this one and any with Dispatch.Manage can act on it.
/// </summary>
internal sealed class CreateAIDispatchConversationHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser)
    : IAppRequestHandler<CreateAIDispatchConversationCommand, Result<AgentConversationDto>>
{
    public Task<Result<AgentConversationDto>> Handle(
        CreateAIDispatchConversationCommand request, CancellationToken ct) =>
        commands.CreateAsync(AgentConversationKind.Dispatch, currentUser.GetUserId(), ct);
}
