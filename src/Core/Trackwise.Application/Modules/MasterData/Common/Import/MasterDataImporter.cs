using FluentValidation;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.MasterData.Common.Import;

internal sealed class MasterDataImporter(
    IMediator mediator,
    ITenantUnitOfWork tenantUow,
    IEnumerable<IMasterDataRowImporter> rowImporters,
    ILogger<MasterDataImporter> logger) : IMasterDataImporter
{
    /// <summary>
    /// The whole file is held in memory, every row goes through the full pipeline, and the lot sits
    /// in one transaction - so the ceiling is about how long a steward's request may hold a write
    /// transaction open, not about parser capacity. A legacy list that exceeds it wants splitting
    /// anyway: a steward cannot review five thousand reported rows either.
    /// </summary>
    private const int MaxRows = 5000;

    public async Task<MasterDataImportReport> ImportAsync(
        MasterDataImportRequest request, CancellationToken ct = default)
    {
        var entity = Find(request.EntityType);

        if (entity is null)
        {
            return MasterDataImportReport.Rejected(
                request.EntityType, request.DryRun, 0,
                MasterDataImportIssue.File($"'{request.EntityType}' cannot be imported."));
        }

        var (header, rows, error) = DelimitedTable.Read(request.Content);

        if (error is not null)
        {
            return MasterDataImportReport.Rejected(
                entity.EntityType, request.DryRun, 0, MasterDataImportIssue.File(error));
        }

        var issues = CheckHeader(entity, header);

        if (rows.Count == 0)
        {
            issues.Add(MasterDataImportIssue.File("The file has a header but no rows."));
        }
        else if (rows.Count > MaxRows)
        {
            issues.Add(MasterDataImportIssue.File(
                $"The file has {rows.Count} rows; {MaxRows} is the most one import can carry. Split it."));
        }

        if (issues.Any(i => i.Severity is MasterDataImportSeverity.Error))
        {
            return MasterDataImportReport.Rejected(
                entity.EntityType, request.DryRun, rows.Count, [.. issues]);
        }

        return await RunAsync(entity, request, rows, issues, ct);
    }

    public string? BuildTemplate(string entityType)
    {
        var entity = Find(entityType);

        if (entity is null)
        {
            return null;
        }

        var header = string.Join(",", entity.Columns.Select(c => DelimitedTable.Quote(c.Name)));
        var sample = string.Join(",", entity.Columns.Select(c => DelimitedTable.Quote(c.Example)));

        return $"{header}\n{sample}\n";
    }

    private IMasterDataRowImporter? Find(string entityType) =>
        rowImporters.FirstOrDefault(
            i => string.Equals(i.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A missing required header is fatal; an unrecognised one is not. Extracts arrive with the
    /// columns the source system happened to have, and refusing a file for carrying an extra one
    /// would send the steward back to Excel for no benefit - but they are still told, because a
    /// column silently ignored is how a misspelled header loses a field of data.
    /// </summary>
    private static List<MasterDataImportIssue> CheckHeader(
        IMasterDataRowImporter entity, IReadOnlyList<string> header)
    {
        var present = header.Select(MasterDataImportHeader.Normalize).ToHashSet();
        var known = entity.Columns.Select(c => MasterDataImportHeader.Normalize(c.Name)).ToHashSet();
        var issues = new List<MasterDataImportIssue>();

        var missing = entity.Columns.Where(
            c => c.Required && !present.Contains(MasterDataImportHeader.Normalize(c.Name)));

        foreach (var column in missing)
        {
            issues.Add(MasterDataImportIssue.Field(
                0, column.Name, $"Required column '{column.Name}' is missing."));
        }

        var unknown = header.Where(
            h => h.Length > 0 && !known.Contains(MasterDataImportHeader.Normalize(h)));

        foreach (var extra in unknown)
        {
            issues.Add(new MasterDataImportIssue(
                0, extra, $"Column '{extra}' is not used and will be ignored.",
                MasterDataImportSeverity.Warning));
        }

        return issues;
    }

    private Task<MasterDataImportReport> RunAsync(
        IMasterDataRowImporter entity,
        MasterDataImportRequest request,
        IReadOnlyList<MasterDataImportRow> rows,
        IReadOnlyList<MasterDataImportIssue> headerIssues,
        CancellationToken ct)
    {
        var overrides = request.AcceptDuplicates
            ? new MasterDataImportOverride(true, request.DuplicateReason)
            : MasterDataImportOverride.None;

        // A dry run is this same unit discarded rather than a cheaper imitation of it, which is why
        // the commit decision belongs to the delegate rather than to a rollback after the fact.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Per attempt, not per import: a retry replays this delegate, and counters carried
                // in from outside would report the abandoned attempt's rows on top of this one's.
                var issues = new List<MasterDataImportIssue>(headerIssues);
                var accepted = 0;

                foreach (var row in rows)
                {
                    var outcome = await WriteRowAsync(entity, row, overrides, token);
                    issues.AddRange(outcome.Issues);

                    if (outcome.Ok)
                    {
                        accepted++;
                    }

                    // Soft failures are collected so one dry run names every offending row; the
                    // steward fixes the file once instead of meeting the next error on the next
                    // attempt. A database-level failure cannot be collected - PostgreSQL marks the
                    // transaction aborted and every later statement then fails for a reason that
                    // has nothing to do with its own row - so it stops here, and the report says
                    // where it stopped.
                    if (outcome.Fatal)
                    {
                        issues.Add(MasterDataImportIssue.File(
                            $"Import stopped at line {row.Line}; later rows were not checked."));
                        break;
                    }
                }

                var failed = issues.Any(i => i.Severity is MasterDataImportSeverity.Error);
                var committed = !failed && !request.DryRun;

                logger.LogInformation(
                    "Master data import: {EntityType}, {Rows} row(s), {Accepted} accepted, dry run {DryRun}, committed {Committed}",
                    entity.EntityType, rows.Count, accepted, request.DryRun, committed);

                var report = new MasterDataImportReport
                {
                    EntityType = entity.EntityType,
                    DryRun = request.DryRun,
                    RowsRead = rows.Count,
                    RowsAccepted = accepted,
                    RowsApplied = committed ? accepted : 0,
                    Issues = issues
                };

                return committed
                    ? TransactionOutcome<MasterDataImportReport>.Keep(report)
                    : TransactionOutcome<MasterDataImportReport>.Discard(report);
            },
            ct);
    }

    private async Task<RowOutcome> WriteRowAsync(
        IMasterDataRowImporter entity,
        MasterDataImportRow row,
        MasterDataImportOverride overrides,
        CancellationToken ct)
    {
        var binding = await entity.BindAsync(row, MasterDataImportOverride.None, ct);

        if (binding.Command is null)
        {
            return new RowOutcome(false, binding.Issues.Count > 0
                ? binding.Issues
                : [MasterDataImportIssue.Row(row.Line, "The row could not be read.")]);
        }

        var first = await SendAsync(binding.Command, row, ct);

        if (first.Issue is not null)
        {
            return new RowOutcome(false, [first.Issue], first.Fatal);
        }

        if (first.Result!.IsSuccess)
        {
            return new RowOutcome(true, binding.Issues);
        }

        // Not a duplicate, or the steward has not declared the file distinct: the row stands
        // rejected, and the pipeline's own sentence - which names what it collided with - is what
        // the steward reads.
        if (first.Result.ErrorCode != ErrorCodes.DuplicateMasterDataRecord || !overrides.ConfirmDistinct)
        {
            return new RowOutcome(false, [MasterDataImportIssue.Row(row.Line, first.Result.Error!)]);
        }

        // The probe blocks before the handler runs, so nothing was written and the row can simply
        // be re-offered carrying the declaration. Re-binding rather than mutating the command keeps
        // the override out of IMasterDataWriteCommand's contract, at the cost of one repeated
        // lookup on rows that collided - which are the minority, by construction.
        var retry = await entity.BindAsync(row, overrides, ct);

        if (retry.Command is null)
        {
            return new RowOutcome(false, retry.Issues);
        }

        var second = await SendAsync(retry.Command, row, ct);

        if (second.Issue is not null)
        {
            return new RowOutcome(false, [second.Issue], second.Fatal);
        }

        if (!second.Result!.IsSuccess)
        {
            return new RowOutcome(false, [MasterDataImportIssue.Row(row.Line, second.Result.Error!)]);
        }

        // Reported even though it succeeded. An override applied to a whole file is exactly the
        // decision that has to stay visible afterwards, and a count alone would not say which rows
        // it covered.
        return new RowOutcome(true, [
            MasterDataImportIssue.Warn(
                row.Line, $"Matched an existing record and was created anyway: {first.Result.Error}")
        ]);
    }

    private async Task<SendOutcome> SendAsync(object command, MasterDataImportRow row, CancellationToken ct)
    {
        try
        {
            // Sent through the mediator so the row inherits validation, the duplicate probe and the
            // change log from the same command the steward's form sends. An importer that wrote
            // entities directly would be a second write path, and a bypassable guarantee is none.
            //
            // Untyped, so nothing compile-checks that the command answers with an IResult. A row
            // importer bound to the wrong request type is a wiring mistake, and it should surface
            // as one bad entity rather than as a null reference halfway through a file.
            return await mediator.Send(command, ct) is IResult result
                ? new SendOutcome(result, null)
                : new SendOutcome(
                    null,
                    MasterDataImportIssue.Row(
                        row.Line, $"{command.GetType().Name} did not answer with a result."),
                    Fatal: true);
        }
        catch (ValidationException ex)
        {
            // Thrown before anything reached the database, so the transaction is intact and the
            // remaining rows can still be checked.
            var messages = ex.Errors.Select(e => e.ErrorMessage).Distinct();
            return new SendOutcome(null, MasterDataImportIssue.Row(row.Line, string.Join(" ", messages)));
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Master data import failed at line {Line}", row.Line);

            return new SendOutcome(
                null,
                MasterDataImportIssue.Row(row.Line, "The row was rejected by the database."),
                Fatal: true);
        }
    }

    private sealed record RowOutcome(
        bool Ok, IReadOnlyList<MasterDataImportIssue> Issues, bool Fatal = false);

    private sealed record SendOutcome(
        IResult? Result, MasterDataImportIssue? Issue, bool Fatal = false);
}
