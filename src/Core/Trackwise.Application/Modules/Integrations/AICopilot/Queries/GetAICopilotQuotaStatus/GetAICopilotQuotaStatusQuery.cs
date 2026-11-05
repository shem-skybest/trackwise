using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Queries;

/// <summary>
/// Same payload as <see cref="AIDispatch.Queries.GetAIQuotaStatusQuery"/>, but gated on AICopilot
/// so a tenant with AgenticDispatch disabled can still read the drawer's quota widget.
/// </summary>
[RequiresFeature(TenantFeature.AICopilot)]
public sealed class GetAICopilotQuotaStatusQuery : IQuery<Result<AIQuotaStatusDto>>;
