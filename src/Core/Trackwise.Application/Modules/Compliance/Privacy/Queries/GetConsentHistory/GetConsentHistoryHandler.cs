using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

internal sealed class GetConsentHistoryHandler(
    IMasterUnitOfWork masterUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<GetConsentHistoryQuery, Result<List<ConsentRecordDto>>>
{
    public async Task<Result<List<ConsentRecordDto>>> Handle(GetConsentHistoryQuery req, CancellationToken ct)
    {
        var userId = currentUserService.GetUserId();
        if (userId is null)
        {
            return Result<List<ConsentRecordDto>>.Fail("User not authenticated.");
        }

        var records = await masterUow.Repository<ConsentRecord>()
            .Query()
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        return Result<List<ConsentRecordDto>>.Ok(records.Select(r => r.ToDto()).ToList());
    }
}
