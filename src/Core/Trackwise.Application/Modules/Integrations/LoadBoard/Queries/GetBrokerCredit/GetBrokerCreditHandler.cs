using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.LoadBoard;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Queries;

internal sealed class GetBrokerCreditHandler(
    ITenantUnitOfWork tenantUow,
    IBrokerCreditService brokerCreditService)
    : IAppRequestHandler<GetBrokerCreditQuery, Result<BrokerCreditDto>>
{
    public async Task<Result<BrokerCreditDto>> Handle(GetBrokerCreditQuery req, CancellationToken ct)
    {
        var credit = await brokerCreditService.GetBrokerCreditAsync(req.McNumber, ct);
        if (credit is null)
        {
            return Result<BrokerCreditDto>.Fail($"No credit data available for MC number '{req.McNumber}'");
        }

        if (req.ListingId.HasValue)
        {
            var listing = await tenantUow.Repository<LoadBoardListing>().GetByIdAsync(req.ListingId.Value, ct);
            if (listing is not null)
            {
                listing.BrokerCreditScore = credit.CreditScore;
                listing.BrokerDaysToPay = credit.DaysToPay;
                listing.BrokerCreditCheckedAt = credit.CheckedAt;
                await tenantUow.SaveChangesAsync(ct);
            }
        }

        return Result<BrokerCreditDto>.Ok(credit);
    }
}
