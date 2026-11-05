using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class ResetTenantQuotasHandler(
    IMasterUnitOfWork masterUow) : IAppRequestHandler<ResetTenantQuotasCommand, Result>
{
    public async Task<Result> Handle(ResetTenantQuotasCommand request, CancellationToken ct)
    {
        var tenantRepo = masterUow.Repository<Tenant>();
        List<Tenant> tenants;

        if (request.TenantIds.Count > 0)
        {
            tenants = await tenantRepo.GetListAsync(
                t => request.TenantIds.Contains(t.Id), ct);
        }
        else
        {
            tenants = await tenantRepo.GetListAsync(
                t => t.ConnectionString != null, ct);
        }

        var now = DateTime.UtcNow;
        foreach (var tenant in tenants)
        {
            tenant.QuotaResetAt = now;
        }

        await masterUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
