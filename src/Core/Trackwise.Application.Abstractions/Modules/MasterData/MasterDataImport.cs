namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// One column a steward's file may carry for an entity, and what it means.
/// <para>
/// Declared per entity rather than inferred from the command's properties, because the file is a
/// contract with a person: the header they are told to use has to stay stable when a property is
/// renamed, and it has to carry an explanation the property name cannot.
/// </para>
/// </summary>
/// <param name="Name">Header text, matched case- and punctuation-insensitively.</param>
/// <param name="Required">Whether the header must be present. Says nothing about the value.</param>
/// <param name="Description">What to put in it. Printed in the downloadable template.</param>
/// <param name="Example">A representative value, used as the template's sample row.</param>
public sealed record MasterDataImportColumn(
    string Name,
    bool Required,
    string Description,
    string? Example = null);

/// <summary>One record read out of the file, keyed by header.</summary>
/// <param name="Line">
/// The line the record started on in the original file, 1-based and counting the header. This is
/// the number the steward sees in their spreadsheet, so it is what every issue is reported against
/// - a row index would be off by one from everything they are looking at.
/// </param>
public sealed record MasterDataImportRow(int Line, IReadOnlyDictionary<string, string?> Values)
{
    /// <summary>The trimmed value for a header, or null when absent or blank.</summary>
    public string? this[string column] =>
        Values.TryGetValue(MasterDataImportHeader.Normalize(column), out var value)
        && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    public bool Has(string column) => this[column] is not null;
}

/// <summary>
/// Canonical form of a header, so <c>Country Code</c>, <c>country_code</c> and <c>COUNTRYCODE</c>
/// are one column. Files come out of six systems and a decade of spreadsheets; insisting on exact
/// spelling would fail the first import for a reason nobody can see.
/// </summary>
public static class MasterDataImportHeader
{
    public static string Normalize(string? header) =>
        new(header?.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray() ?? []);
}

public enum MasterDataImportSeverity
{
    /// <summary>Worth telling the steward, but the row still lands.</summary>
    Warning,

    /// <summary>The row cannot be written, so the whole import is refused.</summary>
    Error
}

/// <param name="Line">
/// The file line the issue is about, or 0 when it concerns the file as a whole (a missing header,
/// an unreadable file).
/// </param>
/// <param name="Column">The header at fault, when the issue narrows to one.</param>
public sealed record MasterDataImportIssue(
    int Line,
    string? Column,
    string Message,
    MasterDataImportSeverity Severity = MasterDataImportSeverity.Error)
{
    public static MasterDataImportIssue File(string message) =>
        new(0, null, message);

    public static MasterDataImportIssue Row(int line, string message) =>
        new(line, null, message);

    public static MasterDataImportIssue Field(int line, string column, string message) =>
        new(line, column, message);

    public static MasterDataImportIssue Warn(int line, string message) =>
        new(line, null, message, MasterDataImportSeverity.Warning);
}

/// <param name="EntityType">CLR type name of the entity being loaded, e.g. <c>Terminal</c>.</param>
/// <param name="Content">The raw file text.</param>
/// <param name="DryRun">
/// When true the import runs to completion and is then rolled back. It is the same code path as a
/// real import, deliberately: a rehearsal that skipped the handlers would pass on a file the commit
/// then rejects, which is worse than no rehearsal at all.
/// </param>
/// <param name="AcceptDuplicates">
/// Re-offers rows the duplicate probe blocked, as the steward declaring the whole file distinct.
/// Requires <paramref name="DuplicateReason"/>, and every row it lets through is reported.
/// </param>
public sealed record MasterDataImportRequest(
    string EntityType,
    string Content,
    bool DryRun = true,
    bool AcceptDuplicates = false,
    string? DuplicateReason = null);

/// <summary>
/// What a file would do, or did. The same shape either way, so the dialog that shows a dry run and
/// the one that confirms a commit are one component.
/// </summary>
public sealed record MasterDataImportReport
{
    public required string EntityType { get; init; }

    public required bool DryRun { get; init; }

    /// <summary>Records found in the file, excluding the header and blank lines.</summary>
    public required int RowsRead { get; init; }

    /// <summary>Rows that the pipeline accepted - what a commit would write, or did.</summary>
    public required int RowsAccepted { get; init; }

    /// <summary>
    /// Rows actually committed. Zero on a dry run and zero on any failure, because the import is
    /// all-or-nothing: a half-loaded reference table is worse than an empty one, since nobody can
    /// tell which half is missing.
    /// </summary>
    public required int RowsApplied { get; init; }

    public required IReadOnlyList<MasterDataImportIssue> Issues { get; init; }

    public IReadOnlyList<MasterDataImportIssue> Errors =>
        Issues.Where(i => i.Severity is MasterDataImportSeverity.Error).ToList();

    public bool Succeeded => Errors.Count == 0 && RowsRead > 0;

    public static MasterDataImportReport Rejected(
        string entityType, bool dryRun, int rowsRead, params MasterDataImportIssue[] issues) =>
        new()
        {
            EntityType = entityType,
            DryRun = dryRun,
            RowsRead = rowsRead,
            RowsAccepted = 0,
            RowsApplied = 0,
            Issues = issues
        };
}
