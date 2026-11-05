using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

internal sealed class GetMyDataExportsHandler(
    IMasterUnitOfWork masterUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<GetMyDataExportsQuery, Result<List<DataExportRequestDto>>>
{
    public async Task<Result<List<DataExportRequestDto>>> Handle(GetMyDataExportsQuery req, CancellationToken ct)
    {
        var userId = currentUserService.GetUserId();
        if (userId is null)
        {
            return Result<List<DataExportRequestDto>>.Fail("User not authenticated.");
        }

        var requests = await masterUow.Repository<DataExportRequest>()
            .Query()
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        return Result<List<DataExportRequestDto>>.Ok(requests.Select(r => r.ToDto()).ToList());
    }
}
