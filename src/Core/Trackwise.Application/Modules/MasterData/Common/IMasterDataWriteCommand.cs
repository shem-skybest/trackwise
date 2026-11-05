namespace Trackwise.Application.Modules.MasterData.Common;

/// <summary>
/// Marks a command that creates or edits a master record, so the write pipeline can normalise,
/// probe for duplicates and apply the entity's policy without the handler doing any of it.
/// <para>
/// Implementing this is what opts a command into those stages. Every write path - UI, API, bulk
/// import, ETL - goes through the same pipeline for the same reason the audit lives in an
/// interceptor: so a future caller inherits the behaviour instead of re-implementing it.
/// </para>
/// </summary>
public interface IMasterDataWriteCommand
{
    /// <summary>CLR type name of the entity being written, e.g. <c>Branch</c>.</summary>
    string MasterDataEntityType { get; }

    /// <summary>The record being edited, so it cannot match itself. Null on create.</summary>
    Guid? MasterDataRecordId { get; }

    /// <summary>
    /// Field values the duplicate probe compares, keyed by the entity's
    /// <c>DuplicateRule.Field</c> names.
    /// </summary>
    IReadOnlyDictionary<string, string?> DedupeFields { get; }

    /// <summary>
    /// Set when the steward has seen the candidates and asserts this is genuinely a different
    /// thing. Recorded with <see cref="DistinctReason"/> so the decision is auditable rather than
    /// a silent override.
    /// </summary>
    bool ConfirmedDistinct { get; }

    string? DistinctReason { get; }
}
