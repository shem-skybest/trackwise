using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Containers.Queries;

internal sealed class GetContainersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetContainersQuery, PagedResult<ContainerDto>>
{
    public async Task<PagedResult<ContainerDto>> Handle(GetContainersQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Container>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Number is stored canonical; workfile references and B/Ls are free-form, so they keep
            // the raw term. A box matches through any workfile it has shipped on.
            var number = Container.NormalizeNumber(req.Search);
            var files = tenantUow.Repository<Workfile>().Query()
                .Where(w => w.ReferenceNumber.Contains(req.Search) ||
                            (w.BillOfLadingNumber != null && w.BillOfLadingNumber.Contains(req.Search)))
                .Select(w => w.Id);
            var shippedOnFiles = tenantUow.Repository<WorkfileContainer>().Query()
                .Where(c => c.ContainerId != null && files.Contains(c.WorkfileId))
                .Select(c => c.ContainerId);

            query = query.Where(i => i.Number.Contains(number) || shippedOnFiles.Contains(i.Id));
        }

        if (req.Status.HasValue)
        {
            query = query.Where(i => i.Status == req.Status.Value);
        }

        if (req.ContainerTypeId.HasValue)
        {
            query = query.Where(i => i.ContainerTypeId == req.ContainerTypeId.Value);
        }

        if (req.CurrentTerminalId.HasValue)
        {
            query = query.Where(i => i.CurrentTerminalId == req.CurrentTerminalId.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var containers = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var terminalIds = containers
            .Select(i => i.CurrentTerminalId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        var terminals = await TerminalRefResolver.ResolveAsync(tenantUow, terminalIds, ct);

        var containerTypes = await ContainerTypeLookup.ResolveAsync(
            tenantUow, containers.Select(i => (Guid?)i.ContainerTypeId), ct);

        var dtos = containers
            .Select(i => i.ToDto(
                i.CurrentTerminalId is { } id && terminals.TryGetValue(id, out var t) ? t : null,
                containerTypes.TryGetValue(i.ContainerTypeId, out var type) ? type : null))
            .ToArray();

        return PagedResult<ContainerDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
