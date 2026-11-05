using System.ComponentModel;
using Trackwise.Application.Abstractions.Agents;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using Microsoft.EntityFrameworkCore;
using Container = Trackwise.Domain.Entities.Container;

namespace Trackwise.Infrastructure.AI.Tools.Intermodal;

/// <summary>
/// Looks up an intermodal container by ISO 6346 number (or id), so the agent knows where the box is
/// before assigning the load that carries it.
/// </summary>
internal sealed class GetContainerStatusTool(ITenantUnitOfWork tenantUow)
    : AgentTool<GetContainerStatusTool.Input>, IAgentToolMetadata
{
    internal sealed record Input
    {
        [Description("ISO 6346 container number, e.g. 'MSCU1234567'")]
        public string? ContainerNumber { get; init; }

        [Description("Container ID (GUID) - use only when the number is unknown")]
        public Guid? ContainerId { get; init; }
    }

    public static AgentToolDefinition Definition => new(
        "get_container_status",
        "Look up an intermodal container by its ISO 6346 number. Returns status (Empty, Loaded, AtPort, InTransit, Delivered, Returned), ISO type, laden flag, the latest workfile it shipped on with that move's gross weight, seal and bill of lading, the terminal the box is currently at, and the load it is linked to. Call this for any load that reports a container_number before assigning it.")
    {
        RequiredFeature = TenantFeature.IntermodalContainers,
        RequiredPermission = Permission.Dispatch.View,
        Surfaces = AgentSurfaces.All
    };

    protected override async Task<string> ExecuteAsync(Input input, CancellationToken ct)
    {
        var number = input.ContainerNumber?.Trim();
        Container? container;

        if (!string.IsNullOrEmpty(number))
        {
            // Dispatchers type ISO 6346 numbers in either case, but stored numbers are canonical -
            // so normalise the term and compare exactly, which the unique index can serve.
            var normalized = Container.NormalizeNumber(number);
            container = await tenantUow.Repository<Container>()
                .GetAsync(c => c.Number == normalized, ct);
        }
        else if (input.ContainerId is { } containerId)
        {
            container = await tenantUow.Repository<Container>().GetByIdAsync(containerId, ct);
        }
        else
        {
            return ToolResult.Error("Provide container_number (ISO 6346) or container_id");
        }

        if (container is null)
            return ToolResult.Error($"No container found for {number ?? input.ContainerId?.ToString()}");

        // Container has no Loads collection - the link is one-way from Load.ContainerId.
        var load = await tenantUow.Repository<Load>()
            .GetAsync(l => l.ContainerId == container.Id, ct);

        var terminal = container.CurrentTerminal;

        // Read past the soft-delete filter rather than off container.ContainerType: a container
        // whose type a steward retired still physically is that box, and the navigation property
        // would come back null.
        var containerType = await tenantUow.Repository<ContainerType>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == container.ContainerTypeId, ct);

        // Seal, weight and B/L belong to one move, so they come from the latest workfile the box is on.
        var shipment = await tenantUow.Repository<WorkfileContainer>().Query()
            .Where(c => c.ContainerId == container.Id)
            .Join(tenantUow.Repository<Workfile>().Query(), c => c.WorkfileId, w => w.Id, (c, w) => new
            {
                w.ReferenceNumber,
                w.BillOfLadingNumber,
                w.CreatedAt,
                c.SealNumber,
                c.GrossWeightKg
            })
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return ToolResult.Ok(new
        {
            id = container.Id,
            number = container.Number,
            container_type = containerType?.Code,
            container_type_description = containerType?.Name,
            iso_size_type_code = containerType?.IsoSizeTypeCode,
            status = container.Status.ToString(),
            is_laden = container.IsLaden,
            notes = container.Notes,
            latest_workfile = shipment?.ReferenceNumber,
            gross_weight_kg = shipment?.GrossWeightKg,
            seal_number = shipment?.SealNumber,
            bill_of_lading_number = shipment?.BillOfLadingNumber,
            current_terminal = terminal is null
                ? null
                : new
                {
                    id = terminal.Id,
                    name = terminal.Name,
                    code = terminal.Code,
                    type = terminal.Type.ToString(),
                    country_code = terminal.CountryCode,
                    address = GetTerminalInfoTool.FormatAddress(terminal.Address)
                },
            loaded_at = container.LoadedAt,
            delivered_at = container.DeliveredAt,
            returned_at = container.ReturnedAt,
            linked_load = load is null
                ? null
                : new
                {
                    id = load.Id,
                    number = load.Number,
                    name = load.Name,
                    status = load.Status.ToString()
                }
        });
    }
}
