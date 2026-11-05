using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// One comparison the duplicate probe makes for an entity.
/// </summary>
/// <param name="Field">
/// Logical field name the caller supplies a value for, e.g. <c>Code</c> or <c>Name</c>.
/// </param>
/// <param name="Column">Physical column it maps to. A constant from the policy table, never user input.</param>
/// <param name="Kind">Exact for identifiers, fuzzy for free-text names.</param>
/// <param name="Label">
/// What the steward is told matched, e.g. "name" or "TIN". Shown verbatim in the probe result, so
/// it should read as an explanation rather than a column name.
/// </param>
public sealed record DuplicateRule(
    string Field,
    string Column,
    DuplicateMatchKind Kind,
    string Label);
