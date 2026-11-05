using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Loads a file of master records for one entity.
/// <para>
/// Not an <c>IMasterDataWriteCommand</c>: it writes no record itself. Each row is sent as that
/// entity's own create command, so the duplicate probe and the policy close over the rows rather
/// than over the file.
/// </para>
/// </summary>
public class ImportMasterDataCommand : ICommand<Result<MasterDataImportDto>>
{
    /// <summary>CLR type name of the entity being loaded, e.g. <c>Terminal</c>. From the route.</summary>
    public string EntityType { get; set; } = default!;

    /// <summary>The file's text, delimited by comma, tab, semicolon or pipe.</summary>
    public string Content { get; set; } = default!;

    /// <summary>
    /// Defaults to a rehearsal. A caller has to ask for the write, so the expensive direction is
    /// never the one taken by omission - an API client that forgets the flag gets a report, not
    /// four hundred rows in a live reference table.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Creates rows the duplicate probe blocked, as a declaration that the file is genuinely new
    /// data. Needs <see cref="DuplicateReason"/>, and each row it covers comes back in the report.
    /// </summary>
    public bool AcceptDuplicates { get; set; }

    public string? DuplicateReason { get; set; }
}
