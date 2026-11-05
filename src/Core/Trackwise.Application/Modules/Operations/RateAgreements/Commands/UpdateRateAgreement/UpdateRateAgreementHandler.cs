using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class UpdateRateAgreementHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateRateAgreementCommand, Result<RateAgreementDto>>
{
    public async Task<Result<RateAgreementDto>> Handle(UpdateRateAgreementCommand req, CancellationToken ct)
    {
        var agreement = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);
        if (agreement is null)
        {
            return Result<RateAgreementDto>.Fail($"Could not find rate agreement with ID '{req.Id}'");
        }

        try
        {
            // Extra-drop charges are replaced like the lines below, and for the same reason.
            var chargeRepository = tenantUow.Repository<RateExtraDropCharge>();
            foreach (var old in agreement.ExtraDropCharges.ToList())
            {
                chargeRepository.Delete(old);
            }

            foreach (var charge in agreement.UpdateTerms(
                req.Name, req.EffectiveFrom, req.EffectiveTo, req.ToTerms(), req.Notes))
            {
                await chargeRepository.AddAsync(charge, ct);
            }

            var lineRepository = tenantUow.Repository<RateAgreementLine>();
            foreach (var old in agreement.Lines.ToList())
            {
                lineRepository.Delete(old);
            }

            // The agreement is tracked, so lines reaching it only through the collection would be
            // saved as updates to rows that don't exist. Adding each one (with its surcharges) marks
            // the new rows as inserts.
            foreach (var line in agreement.ReplaceLines(req.Lines.Select(l => l.ToSpec())))
            {
                await lineRepository.AddAsync(line, ct);
            }

            await tenantUow.SaveChangesAsync(ct);
            return Result<RateAgreementDto>.Ok(await RateAgreementReader.ToDtoAsync(tenantUow, agreement, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RateAgreementDto>.Fail(ex.Message);
        }
    }
}
