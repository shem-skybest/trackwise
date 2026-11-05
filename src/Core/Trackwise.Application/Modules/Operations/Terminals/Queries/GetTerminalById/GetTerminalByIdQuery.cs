using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Terminals.Queries;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class GetTerminalByIdQuery : IQuery<Result<TerminalDto>>, IHaveId
{
    public Guid Id { get; set; }
}
