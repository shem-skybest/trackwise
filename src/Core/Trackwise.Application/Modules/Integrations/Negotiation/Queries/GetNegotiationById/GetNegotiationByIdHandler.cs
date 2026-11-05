using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

internal sealed class GetNegotiationByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetNegotiationByIdQuery, Result<RateNegotiationDto>>
{
    public async Task<Result<RateNegotiationDto>> Handle(GetNegotiationByIdQuery req, CancellationToken ct)
    {
        var negotiation = await tenantUow.Repository<RateNegotiation>().GetByIdAsync(req.Id, ct);
        if (negotiation is null)
        {
            return Result<RateNegotiationDto>.Fail($"Could not find a negotiation with ID '{req.Id}'");
        }

        var listing = await tenantUow.Repository<LoadBoardListing>()
            .GetByIdAsync(negotiation.LoadBoardListingId, ct);

        var messages = await tenantUow.Repository<NegotiationMessage>().Query()
            .Where(m => m.NegotiationId == negotiation.Id)
            .OrderBy(m => m.Sequence)
            .ProjectToDto()
            .ToListAsync(ct);

        var dto = negotiation.ToDto(listing);
        dto.Messages = messages;
        return Result<RateNegotiationDto>.Ok(dto);
    }
}
