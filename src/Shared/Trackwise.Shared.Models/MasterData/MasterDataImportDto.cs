namespace Trackwise.Shared.Models;

/// <summary>
/// What a file would do, or did. A dry run and a commit return the same shape, because a dry run
/// is the same import rolled back - so one dialog can show both.
/// </summary>
public class MasterDataImportDto
{
    public required string EntityType { get; set; }

    /// <summary>True when nothing was kept, whatever the counts below say.</summary>
    public bool DryRun { get; set; }

    /// <summary>True when the file has no errors - i.e. a commit would land, or did.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Records in the file, excluding the header and blank lines.</summary>
    public int RowsRead { get; set; }

    /// <summary>Rows the write pipeline accepted.</summary>
    public int RowsAccepted { get; set; }

    /// <summary>
    /// Rows actually written. Zero on a dry run, and zero on any failure - an import is
    /// all-or-nothing, so a partially loaded list is never a possible outcome.
    /// </summary>
    public int RowsApplied { get; set; }

    public List<MasterDataImportIssueDto> Issues { get; set; } = [];
}

/// <summary>
/// One thing wrong with, or worth saying about, the file.
/// </summary>
public class MasterDataImportIssueDto
{
    /// <summary>
    /// The line in the steward's own file, counting the header, or 0 when it is about the file as
    /// a whole. This is the number they can go and look at.
    /// </summary>
    public int Line { get; set; }

    /// <summary>The column header at fault, when the issue narrows to one.</summary>
    public string? Column { get; set; }

    public required string Message { get; set; }

    /// <summary>True when this is what stopped the import; false when it is only advisory.</summary>
    public bool IsError { get; set; }
}
