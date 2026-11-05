using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Marks a command that retires a master record - clearing its active flag or soft-deleting it - so
/// the write pipeline can check what still depends on it before the handler runs.
/// <para>
/// The counterpart to <see cref="IMasterDataWriteCommand"/>, and implemented for the same reason:
/// the check belongs to the pipeline, so bulk import and the ETL inherit it rather than each
/// growing its own copy. A command that merely edits a record reports
/// <see cref="MasterDataDeactivation.None"/> and costs nothing.
/// </para>
/// </summary>
public interface IMasterDataDeactivateCommand
{
    /// <summary>CLR type name of the entity being retired, e.g. <c>Branch</c>.</summary>
    string MasterDataEntityType { get; }

    Guid MasterDataRecordId { get; }

    /// <summary>
    /// What this command does to the record's availability. Commands that both edit and deactivate
    /// - an update carrying <c>IsActive = false</c> - report the retirement.
    /// </summary>
    MasterDataDeactivation Deactivation { get; }

    /// <summary>
    /// Set when the steward has seen what still depends on the record and is retiring it anyway.
    /// Requires <see cref="DependencyReason"/>, so the override is auditable rather than silent -
    /// the same bargain the duplicate probe strikes.
    /// </summary>
    bool ConfirmedDespiteDependents { get; }

    string? DependencyReason { get; }
}
