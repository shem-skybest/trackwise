using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Queries;

[RequiresFeature(TenantFeature.AICopilot)]
public class GetAICopilotConversationsQuery : PagedQuery, IQuery<PagedResult<AgentConversationDto>>;
