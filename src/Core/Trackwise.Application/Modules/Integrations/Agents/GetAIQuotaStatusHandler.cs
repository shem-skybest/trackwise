using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Application.Modules.Integrations.AICopilot.Queries;
using Trackwise.Application.Modules.Integrations.AIDispatch.Queries;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Agents;

/// <summary>One handler for both surfaces' quota queries; only the feature gate differs per query.</summary>
internal sealed class GetAIQuotaStatusHandler(
    IAIQuotaService quotaService,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetAIQuotaStatusQuery, Result<AIQuotaStatusDto>>,
        IAppRequestHandler<GetAICopilotQuotaStatusQuery, Result<AIQuotaStatusDto>>
{
    public Task<Result<AIQuotaStatusDto>> Handle(GetAIQuotaStatusQuery request, CancellationToken ct) =>
        GetStatusAsync(ct);

    public Task<Result<AIQuotaStatusDto>> Handle(GetAICopilotQuotaStatusQuery request, CancellationToken ct) =>
        GetStatusAsync(ct);

    private async Task<Result<AIQuotaStatusDto>> GetStatusAsync(CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();
        var status = await quotaService.GetQuotaStatusAsync(tenant.Id, ct);

        return Result<AIQuotaStatusDto>.Ok(status.ToDto());
    }
}
