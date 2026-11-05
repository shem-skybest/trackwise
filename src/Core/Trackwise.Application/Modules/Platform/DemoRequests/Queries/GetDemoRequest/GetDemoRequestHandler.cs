using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Queries;

internal sealed class GetDemoRequestHandler(
    IMasterUnitOfWork masterUow) : IAppRequestHandler<GetDemoRequestQuery, Result<DemoRequestDto>>
{
    public async Task<Result<DemoRequestDto>> Handle(GetDemoRequestQuery req, CancellationToken ct)
    {
        var demoRequest = await masterUow.Repository<DemoRequest>().GetByIdAsync(req.Id, ct);

        if (demoRequest is null)
        {
            return Result<DemoRequestDto>.Fail($"Demo request with ID '{req.Id}' not found");
        }

        return Result<DemoRequestDto>.Ok(demoRequest.ToDto());
    }
}
