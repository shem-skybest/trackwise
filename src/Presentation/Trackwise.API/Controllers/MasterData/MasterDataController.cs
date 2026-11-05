using System.Text;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Cross-entity master-data operations, as opposed to the per-entity controllers.
/// </summary>
[ApiController]
[Route("master-data")]
[Produces("application/json")]
public class MasterDataController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns records that look like the one being entered. Read-only and safe to call on every
    /// keystroke - the UI shows candidates before the steward saves, which is the point.
    /// </summary>
    [HttpPost("duplicates/probe", Name = "ProbeDuplicates")]
    [ProducesResponseType(typeof(DuplicateProbeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> ProbeDuplicates(
        [FromBody] ProbeDuplicatesQuery request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Loads a file of master records for one entity, rehearsing by default.
    /// <para>
    /// The write is opt-in - <c>commit=true</c> - rather than the rehearsal being opt-out. Both
    /// directions are one flag, but only one of them is unrecoverable if a client omits it, and a
    /// form field that fails to bind binds as <c>false</c>.
    /// </para>
    /// <para>
    /// Its own route rather than a sub-resource of <c>master-data/ports</c>: the entity segment is
    /// a CLR type name from the policy table, not the plural resource name, and one import route
    /// serves every entity. A per-entity route would mean two more endpoints for each list added.
    /// </para>
    /// </summary>
    [HttpPost("imports/{entityType}", Name = "ImportMasterData")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    [ProducesResponseType(typeof(MasterDataImportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Import(
        string entityType,
        IFormFile file,
        [FromForm] bool commit = false,
        [FromForm] bool acceptDuplicates = false,
        [FromForm] string? duplicateReason = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ErrorResponse.FromResult(Result.Fail("Attach a file to import.")));
        }

        if (!IsDelimitedText(file))
        {
            return BadRequest(ErrorResponse.FromResult(
                Result.Fail("Import a CSV, TSV or plain-text file. Save the workbook as CSV first.")));
        }

        await using var stream = file.OpenReadStream();

        // A byte-order mark decides the encoding when the export carries one; UTF-8 is the fallback
        // rather than the system code page, so the same file reads identically on every host.
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var command = new ImportMasterDataCommand
        {
            EntityType = entityType,
            Content = await reader.ReadToEndAsync(ct),
            DryRun = !commit,
            AcceptDuplicates = acceptDuplicates,
            DuplicateReason = duplicateReason
        };

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Extension and declared type must both be delimited text. The content is parsed and thrown
    /// away rather than stored, but an <c>.xlsx</c> read as text produces a page of parser noise
    /// instead of the one sentence that tells the steward to export as CSV.
    /// </summary>
    private static bool IsDelimitedText(IFormFile file)
    {
        string[] extensions = [".csv", ".tsv", ".txt"];
        string[] types = ["text/csv", "text/plain", "text/tab-separated-values", "application/csv"];

        return extensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)
               && (string.IsNullOrEmpty(file.ContentType)
                   || types.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)

                   // Windows registers .csv to Excel, so a browser on a machine with Office
                   // installed sends this for a plain CSV. Refusing it would fail the common case.
                   || file.ContentType.StartsWith("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The expected file for an entity: its headers and one sample row. The only place a steward is
    /// told what the columns are, so it is served from the same declaration the import reads.
    /// </summary>
    [HttpGet("imports/{entityType}/template", Name = "GetMasterDataImportTemplate")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public IActionResult GetImportTemplate(string entityType, [FromServices] IMasterDataImporter importer)
    {
        var template = importer.BuildTemplate(entityType);

        if (template is null)
        {
            return NotFound(ErrorResponse.FromResult(
                Result.Fail($"'{entityType}' cannot be imported.")));
        }

        // The BOM is what makes Excel open the file as UTF-8 instead of the local code page, which
        // is the difference between a port named Ñuñoa and one named NuÃ±oa.
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(template)).ToArray(),
            "text/csv",
            $"{entityType.ToLowerInvariant()}-import-template.csv");
    }
}
