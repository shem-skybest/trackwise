using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class ReviseRateAgreementHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ReviseRateAgreementCommand, Result<RateAgreementDto>>
{
    public async Task<Result<RateAgreementDto>> Handle(ReviseRateAgreementCommand req, CancellationToken ct)
    {
        var source = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);
        if (source is null)
        {
            return Result<RateAgreementDto>.Fail($"Could not find rate agreement with ID '{req.Id}'");
        }

        if (req.EffectiveFrom <= source.EffectiveFrom)
        {
            return Result<RateAgreementDto>.Fail(
                $"A revision must start after {source.EffectiveFrom:yyyy-MM-dd}, when '{source.Name}' starts.");
        }

        try
        {
            var revision = source.CreateRevision(req.Name, req.EffectiveFrom);
            await tenantUow.Repository<RateAgreement>().AddAsync(revision, ct);
            await tenantUow.SaveChangesAsync(ct);

            return Result<RateAgreementDto>.Ok(await RateAgreementReader.ToDtoAsync(tenantUow, revision, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RateAgreementDto>.Fail(ex.Message);
        }
    }
}
