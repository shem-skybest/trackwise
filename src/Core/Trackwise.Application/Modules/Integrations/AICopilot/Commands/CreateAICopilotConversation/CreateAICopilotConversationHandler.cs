using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class CreateAICopilotConversationHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser)
    : IAppRequestHandler<CreateAICopilotConversationCommand, Result<AgentConversationDto>>
{
    public Task<Result<AgentConversationDto>> Handle(
        CreateAICopilotConversationCommand request, CancellationToken ct) =>
        commands.CreateAsync(AgentConversationKind.Copilot, currentUser.GetUserId(), ct);
}
