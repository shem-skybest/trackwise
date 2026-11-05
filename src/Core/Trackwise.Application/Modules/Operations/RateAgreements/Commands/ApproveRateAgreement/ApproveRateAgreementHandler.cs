using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class ApproveRateAgreementHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<ApproveRateAgreementCommand, Result<RateAgreementDto>>
{
    public async Task<Result<RateAgreementDto>> Handle(ApproveRateAgreementCommand req, CancellationToken ct)
    {
        var agreement = await tenantUow.Repository<RateAgreement>().GetByIdAsync(req.Id, ct);
        if (agreement is null)
        {
            return Result<RateAgreementDto>.Fail($"Could not find rate agreement with ID '{req.Id}'");
        }

        var running = await tenantUow.Repository<RateAgreement>().Query()
            .Where(a => a.CustomerId == agreement.CustomerId
                && a.Id != agreement.Id
                && a.Status == RateAgreementStatus.Active)
            .ToListAsync(ct);

        // One agreement per date. One that starts on or after this one would leave the two
        // overlapping from its start onwards, and which of them "wins" is a decision for a person.
        var later = running.FirstOrDefault(a => a.EffectiveFrom >= agreement.EffectiveFrom);
        if (later is not null)
        {
            return Result<RateAgreementDto>.Fail(
                $"'{later.Name}' is already in force from {later.EffectiveFrom:yyyy-MM-dd}. "
                + "Start this agreement after it, or withdraw it first.");
        }

        try
        {
            foreach (var previous in running.Where(a => a.EffectiveTo is null || a.EffectiveTo >= agreement.EffectiveFrom))
            {
                previous.SupersedeFrom(agreement.EffectiveFrom);
            }

            agreement.Approve(currentUser.GetUserName(), DateTime.UtcNow);
            await tenantUow.SaveChangesAsync(ct);

            return Result<RateAgreementDto>.Ok(await RateAgreementReader.ToDtoAsync(tenantUow, agreement, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<RateAgreementDto>.Fail(ex.Message);
        }
    }
}
