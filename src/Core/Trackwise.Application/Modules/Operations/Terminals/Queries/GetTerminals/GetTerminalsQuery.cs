using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Terminals.Queries;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class GetTerminalsQuery : SearchableQuery, IQuery<PagedResult<TerminalDto>>
{
    public TerminalType? Type { get; set; }
    public string? CountryCode { get; set; }

    /// <summary>
    /// Narrows to terminals still offered for new work. A picker should pass <c>true</c>: an
    /// inactive port still resolves for history, but selecting one for a new load is a mistake.
    /// </summary>
    public bool? IsActive { get; set; }
}
