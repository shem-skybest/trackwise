using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Terminals.Queries;

internal sealed class GetTerminalByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetTerminalByIdQuery, Terminal, TerminalDto>(tenantUow)
{
    protected override TerminalDto MapToDto(Terminal entity) => entity.ToDto();
}
