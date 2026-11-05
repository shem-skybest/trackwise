using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// The field-level history of one master record, newest first. Works for any master entity, since
/// the log is keyed by entity type and id rather than by a foreign key per table.
/// </summary>
public class GetMasterDataChangeLogQuery : PagedQuery, IQuery<PagedResult<MasterDataChangeLogDto>>
{
    /// <summary>
    /// CLR type name of the entity, e.g. <c>Branch</c>. Set by the controller from its route, never
    /// by the caller - so it is nullable, because MVC treats a non-nullable reference property as
    /// implicitly required at bind time and would reject the request before the controller runs.
    /// The validator enforces its presence instead.
    /// </summary>
    public string? EntityType { get; set; }

    public Guid EntityId { get; set; }
}
