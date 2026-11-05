using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class GetRateAgreementByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetRateAgreementByIdQuery, Result<RateAgreementDto>>
{
    public async Task<Result<RateAgreementDto>> Handle(GetRateAgreementByIdQuery req, CancellationToken ct)
    {
        var agreement = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);

        return agreement is null
            ? Result<RateAgreementDto>.Fail($"Could not find rate agreement with ID '{req.Id}'")
            : Result<RateAgreementDto>.Ok(await RateAgreementReader.ToDtoAsync(tenantUow, agreement, ct));
    }
}
