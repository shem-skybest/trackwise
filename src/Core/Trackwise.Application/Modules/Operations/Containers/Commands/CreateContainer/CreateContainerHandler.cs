using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

internal sealed class CreateContainerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateContainerCommand, Result<ContainerDto>>
{
    public async Task<Result<ContainerDto>> Handle(CreateContainerCommand req, CancellationToken ct)
    {
        // Stored numbers are canonical, so the search term must be too, or "mscu1234567" reads as
        // free and then collides on insert.
        var number = Container.NormalizeNumber(req.Number);
        var existing = await tenantUow.Repository<Container>().GetAsync(i => i.Number == number, ct);

        if (existing is not null)
        {
            return Result<ContainerDto>.Fail($"A container with number '{number}' already exists");
        }

        // The type is a required foreign key now, not a self-validating enum member, so an
        // unknown id has to be caught here rather than surfacing as a constraint violation.
        var containerType = await tenantUow.Repository<ContainerType>()
            .GetByIdAsync(req.ContainerTypeId, ct);

        if (containerType is null)
        {
            return Result<ContainerDto>.Fail(
                $"Could not find container type with ID '{req.ContainerTypeId}'");
        }

        Terminal? terminal = null;
        if (req.CurrentTerminalId.HasValue)
        {
            terminal = await tenantUow.Repository<Terminal>().GetByIdAsync(req.CurrentTerminalId.Value, ct);
            if (terminal is null)
            {
                return Result<ContainerDto>.Fail($"Could not find terminal with ID '{req.CurrentTerminalId}'");
            }
        }

        var container = new Container
        {
            Number = number,
            ContainerTypeId = containerType.Id,
            IsLaden = req.IsLaden,
            CurrentTerminalId = terminal?.Id,
            CurrentTerminal = terminal,
            Notes = req.Notes
        };

        // Initial status - bypass state machine via force, since the container starts at the requested status.
        if (req.Status != ContainerStatus.Empty)
        {
            container.UpdateStatus(req.Status, force: true);
        }

        await tenantUow.Repository<Container>().AddAsync(container, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<ContainerDto>.Ok(container.ToDto(
            terminal is null ? null : new TerminalRef(terminal.Name, terminal.Code),
            new ContainerTypeRef(containerType.Code, containerType.Name)));
    }
}
