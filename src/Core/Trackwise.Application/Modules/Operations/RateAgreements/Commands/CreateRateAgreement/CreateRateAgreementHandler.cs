using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class CreateRateAgreementHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateRateAgreementCommand, Result<RateAgreementDto>>
{
    public async Task<Result<RateAgreementDto>> Handle(CreateRateAgreementCommand req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Customer>().GetByIdAsync(req.CustomerId, ct) is null)
        {
            return Result<RateAgreementDto>.Fail($"Could not find customer with ID '{req.CustomerId}'");
        }

        try
        {
            var agreement = RateAgreement.Create(
                req.CustomerId, req.Name, req.EffectiveFrom, req.EffectiveTo, req.ToTerms(), req.Notes);

            agreement.ReplaceLines(req.Lines.Select(l => l.ToSpec()));

            // A new aggregate added whole: the lines and surcharges ride in as Added with it.
            await tenantUow.Repository<RateAgreement>().AddAsync(agreement, ct);
            await tenantUow.SaveChangesAsync(ct);

            return Result<RateAgreementDto>.Ok(await RateAgreementReader.ToDtoAsync(tenantUow, agreement, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RateAgreementDto>.Fail(ex.Message);
        }
    }
}
