namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// The entity-specific half of a bulk import: turning one file row into the ordinary create command
/// for that entity.
/// <para>
/// This is all an entity has to supply. Parsing, the dry run, the transaction, the duplicate
/// retry and the report are identical for every entity and live in <see cref="IMasterDataImporter"/>
/// - an importer that hand-rolled the write would bypass validation, the duplicate probe and the
/// change log, which is the whole reason imports route through the same command the UI sends.
/// </para>
/// </summary>
public interface IMasterDataRowImporter
{
    /// <summary>CLR type name of the entity, matching the policy's <c>EntityType</c>.</summary>
    string EntityType { get; }

    /// <summary>
    /// Headers this entity understands, in the order the template writes them.
    /// </summary>
    IReadOnlyList<MasterDataImportColumn> Columns { get; }

    /// <summary>
    /// Builds the create command for one row, or rejects the row with issues naming the columns at
    /// fault.
    /// <para>
    /// Only shape and lookup failures belong here - a code that is not a known branch, a word that
    /// is not a terminal type. Field rules are the command's validator's job and must not be
    /// restated, or the file and the form start disagreeing about what is legal.
    /// </para>
    /// </summary>
    Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default);
}

/// <summary>
/// Declarations the steward attached to the whole file, which each row's command has to carry for
/// the pipeline to honour them.
/// </summary>
/// <param name="ConfirmDistinct">Set on the retry of a row the duplicate probe blocked.</param>
public sealed record MasterDataImportOverride(bool ConfirmDistinct = false, string? Reason = null)
{
    public static MasterDataImportOverride None { get; } = new();
}

/// <summary>A bound row: either a command to send, or the reasons it could not be built.</summary>
public sealed record MasterDataRowBinding
{
    /// <summary>The create command, sent through <c>IMediator</c> like any other write.</summary>
    public object? Command { get; init; }

    public IReadOnlyList<MasterDataImportIssue> Issues { get; init; } = [];

    public static MasterDataRowBinding For(object command) => new() { Command = command };

    public static MasterDataRowBinding Rejected(params MasterDataImportIssue[] issues) =>
        new() { Issues = issues };
}
