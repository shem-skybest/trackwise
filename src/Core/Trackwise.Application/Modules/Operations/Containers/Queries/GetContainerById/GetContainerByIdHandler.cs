using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Containers.Queries;

/// <summary>
/// Does not use <c>GetTenantEntityByIdHandler</c>: its mapping hook is synchronous, and the
/// terminal name has to be resolved past the soft-delete query filter, which the navigation
/// property can no longer do now that a port can be retired.
/// </summary>
internal sealed class GetContainerByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetContainerByIdQuery, Result<ContainerDto>>
{
    public async Task<Result<ContainerDto>> Handle(GetContainerByIdQuery req, CancellationToken ct)
    {
        var container = await tenantUow.Repository<Container>().GetByIdAsync(req.Id, ct);

        if (container is null)
        {
            return Result<ContainerDto>.Fail($"Could not find a container with ID '{req.Id}'");
        }

        TerminalRef? terminal = null;
        if (container.CurrentTerminalId is { } terminalId)
        {
            var terminals = await TerminalRefResolver.ResolveAsync(tenantUow, [terminalId], ct);
            terminal = terminals.TryGetValue(terminalId, out var found) ? found : null;
        }

        var types = await ContainerTypeLookup.ResolveAsync(tenantUow, [container.ContainerTypeId], ct);
        var dto = container.ToDto(
            terminal, types.TryGetValue(container.ContainerTypeId, out var type) ? type : null);

        dto.Shipments = await tenantUow.Repository<WorkfileContainer>().Query()
            .AsNoTracking()
            .Where(c => c.ContainerId == container.Id)
            .Join(tenantUow.Repository<Workfile>().Query(), c => c.WorkfileId, w => w.Id, (c, w) => new ContainerShipmentDto
            {
                WorkfileId = w.Id,
                ReferenceNumber = w.ReferenceNumber,
                BillOfLadingNumber = w.BillOfLadingNumber,
                Status = w.Status,
                SealNumber = c.SealNumber,
                GrossWeightKg = c.GrossWeightKg,
                CreatedAt = w.CreatedAt
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return Result<ContainerDto>.Ok(dto);
    }
}
