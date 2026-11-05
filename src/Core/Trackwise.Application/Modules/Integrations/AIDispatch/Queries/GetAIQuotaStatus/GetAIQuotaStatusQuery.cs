using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

/// <summary>The tenant-wide AI budget, served to both the dispatch board and the copilot drawer.</summary>
[RequiresFeature(TenantFeature.AgenticDispatch)]
public sealed class GetAIQuotaStatusQuery : IQuery<Result<AIQuotaStatusDto>>;
