using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One entry in a master record's field-level history: what changed, from what, to what, by whom.
/// </summary>
public class MasterDataChangeLogDto
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string? EntityCode { get; set; }
    public MasterDataChangeType ChangeType { get; set; }

    /// <summary>Null for whole-record events - a create, delete, restore or merge.</summary>
    public string? Field { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public MasterDataSourceSystem? SourceSystem { get; set; }
}
