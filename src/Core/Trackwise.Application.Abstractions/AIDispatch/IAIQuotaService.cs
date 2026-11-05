using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.AIDispatch;

public interface IAIQuotaService
{
    Task<AIQuotaStatus> GetQuotaStatusAsync(Guid tenantId, CancellationToken ct = default);
}
