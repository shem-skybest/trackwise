using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Asks "does something like this already exist?" without writing anything, so the UI can warn a
/// steward while they are still typing.
/// </summary>
public class ProbeDuplicatesQuery : IQuery<Result<DuplicateProbeDto>>
{
    /// <summary>CLR type name of the entity, e.g. <c>Branch</c>.</summary>
    public string? EntityType { get; set; }

    /// <summary>Field values to compare, keyed by the entity's configured duplicate-rule fields.</summary>
    public Dictionary<string, string?> Fields { get; set; } = [];

    /// <summary>The record being edited, so it never matches itself.</summary>
    public Guid? ExcludeId { get; set; }
}
