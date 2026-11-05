using FluentValidation;
using FluentValidation.Results;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common.Import;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Two promises define a bulk import, and both are easy to break without noticing. A dry run has to
/// be the real import rolled back - a cheaper imitation would pass files the commit then rejects -
/// and an import has to be all-or-nothing, because a half-loaded reference list is the one outcome
/// nobody can untangle afterwards: the rows that failed look exactly like the rows nobody sent.
/// </summary>
public class MasterDataImporterTests
{
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly FakeRowImporter entity = new();

    private int commits;
    private int discards;

    /// <summary>
    /// A substitute has no execution strategy and therefore no opinion about a transaction, which
    /// is how this feature stayed green while failing on its first real save against a database.
    /// The least a double can do is run the unit and record whether it was kept - that decision is
    /// what every assertion about a dry run is really about.
    /// </summary>
    public MasterDataImporterTests()
    {
        tenantUow.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<TransactionOutcome<MasterDataImportReport>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(RunUnit);
    }

    private async Task<MasterDataImportReport> RunUnit(CallInfo call)
    {
        var work = call
            .Arg<Func<CancellationToken, Task<TransactionOutcome<MasterDataImportReport>>>>();

        var outcome = await work(CancellationToken.None);

        if (outcome.Commit)
        {
            commits++;
        }
        else
        {
            discards++;
        }

        return outcome.Value;
    }

    private const string TwoRows = "Code,Name\nPHMNL,Manila\nPHCEB,Cebu\n";

    private MasterDataImporter Sut() =>
        new(mediator, tenantUow, [entity], NullLogger<MasterDataImporter>.Instance);

    private Task<MasterDataImportReport> Import(
        string content, bool dryRun = true, bool acceptDuplicates = false, string? reason = null) =>
        Sut().ImportAsync(new MasterDataImportRequest("Widget", content, dryRun, acceptDuplicates, reason));

    /// <summary>Makes the mediator answer each row in turn with the given results.</summary>
    private void PipelineReturns(params IResult[] results)
    {
        var next = 0;
        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(_ => results[Math.Min(next++, results.Length - 1)]);
    }

    private static Result Ok() => Result.Ok();

    private static Result Duplicate() =>
        Result.Fail("This looks like an existing widget: PHMNL - Manila.", ErrorCodes.DuplicateMasterDataRecord);

    [Fact]
    public async Task CleanDryRun_RollsBackAndAppliesNothing()
    {
        PipelineReturns(Ok());

        var report = await Import(TwoRows);

        Assert.True(report.Succeeded);
        Assert.Equal(2, report.RowsRead);
        Assert.Equal(2, report.RowsAccepted);
        Assert.Equal(0, report.RowsApplied);
        Assert.Equal(1, discards);
        Assert.Equal(0, commits);
    }

    [Fact]
    public async Task DryRun_StillSendsEveryRowThroughThePipeline()
    {
        // The point of the rehearsal. A dry run that skipped the handlers would pass a file whose
        // commit then fails on a code clash or a missing branch - worse than no rehearsal at all.
        PipelineReturns(Ok());

        await Import(TwoRows);

        await mediator.Received(2).Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanCommit_Commits()
    {
        PipelineReturns(Ok());

        var report = await Import(TwoRows, dryRun: false);

        Assert.Equal(2, report.RowsApplied);
        Assert.Equal(1, commits);
        Assert.Equal(0, discards);
    }

    [Fact]
    public async Task OneBadRow_RollsBackTheRowsThatPassed()
    {
        // All-or-nothing. Row 2 was accepted by the pipeline and is still not kept, because the
        // file as a whole failed.
        PipelineReturns(Result.Fail("Name is required."), Ok());

        var report = await Import(TwoRows, dryRun: false);

        Assert.False(report.Succeeded);
        Assert.Equal(0, report.RowsApplied);
        Assert.Equal(1, discards);
        Assert.Equal(0, commits);
    }

    [Fact]
    public async Task RejectedRow_IsReportedAgainstItsLineInTheFile()
    {
        PipelineReturns(Ok(), Result.Fail("Name is required."));

        var report = await Import(TwoRows);

        var issue = Assert.Single(report.Errors);

        // Line 3, not row 2: the number has to match what the steward sees in their spreadsheet.
        Assert.Equal(3, issue.Line);
        Assert.Contains("Name is required.", issue.Message);
    }

    [Fact]
    public async Task EveryOffendingRowIsNamedInOnePass()
    {
        // The dry run is worth running once, not once per error.
        PipelineReturns(Result.Fail("Name is required."));

        var report = await Import(TwoRows);

        Assert.Equal(new[] { 2, 3 }, report.Errors.Select(i => i.Line));
    }

    [Fact]
    public async Task MissingRequiredColumn_FailsTheFileBeforeAnyRowIsSent()
    {
        var report = await Import("Code\nPHMNL\n");

        var issue = Assert.Single(report.Errors);
        Assert.Equal(0, issue.Line);
        Assert.Equal("Name", issue.Column);
        await mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, commits + discards);
    }

    [Fact]
    public async Task UnrecognisedColumn_WarnsButDoesNotStopTheImport()
    {
        // Extracts carry whatever columns the source system had. Refusing the file would send the
        // steward back to Excel for nothing - but a silently ignored column is how a misspelled
        // header quietly loses a field, so they are told.
        PipelineReturns(Ok());

        var report = await Import("Code,Name,Tonnage\nPHMNL,Manila,40\n", dryRun: false);

        Assert.True(report.Succeeded);
        Assert.Equal(1, report.RowsApplied);
        var warning = Assert.Single(report.Issues, i => i.Severity is MasterDataImportSeverity.Warning);
        Assert.Contains("Tonnage", warning.Message);
    }

    [Fact]
    public async Task HeaderWithNoRows_IsAFailureNotAnEmptySuccess()
    {
        var report = await Import("Code,Name\n");

        Assert.False(report.Succeeded);
        Assert.Single(report.Errors);
    }

    [Fact]
    public async Task RowBoundToNothing_IsReportedWithoutReachingThePipeline()
    {
        PipelineReturns(Ok());
        entity.Reject = row => row.Line == 3;

        var report = await Import(TwoRows);

        Assert.Equal(3, Assert.Single(report.Errors).Line);
        await mediator.Received(1).Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateRow_IsRejectedWithThePipelinesOwnExplanation()
    {
        PipelineReturns(Duplicate());

        var report = await Import(TwoRows);

        Assert.False(report.Succeeded);

        // The probe's sentence names what it collided with, which is the only thing that lets the
        // steward decide between opening that record and declaring this one distinct.
        Assert.Contains("existing widget", report.Errors[0].Message);
    }

    [Fact]
    public async Task AcceptDuplicates_ReoffersTheRowAsDeclaredDistinct()
    {
        var results = new Queue<IResult>([Duplicate(), Ok()]);
        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(_ => results.Dequeue());

        var report = await Import(
            "Code,Name\nPHMNL,Manila\n", dryRun: false, acceptDuplicates: true, reason: "New terminal");

        Assert.True(report.Succeeded);
        Assert.Equal(1, report.RowsApplied);
        Assert.Equal("New terminal", entity.LastOverride.Reason);
        Assert.True(entity.LastOverride.ConfirmDistinct);
    }

    [Fact]
    public async Task AnOverriddenRow_IsStillReported()
    {
        // A blanket override over a whole file is exactly the decision that has to stay visible
        // afterwards, and a count alone would not say which rows it covered.
        var results = new Queue<IResult>([Duplicate(), Ok()]);
        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(_ => results.Dequeue());

        var report = await Import(
            "Code,Name\nPHMNL,Manila\n", dryRun: false, acceptDuplicates: true, reason: "New terminal");

        var warning = Assert.Single(report.Issues);
        Assert.Equal(2, warning.Line);
        Assert.Equal(MasterDataImportSeverity.Warning, warning.Severity);
    }

    [Fact]
    public async Task ValidationFailure_IsCollectedAndTheNextRowIsStillChecked()
    {
        // FluentValidation throws before anything reaches the database, so the transaction is
        // intact and there is no reason to stop looking.
        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new ValidationException(
                [new ValidationFailure("Name", "Name must not be empty.")]));

        var report = await Import(TwoRows);

        Assert.Equal(2, report.Errors.Count);
        Assert.Contains("Name must not be empty.", report.Errors[0].Message);
    }

    [Fact]
    public async Task DatabaseFailure_StopsTheRunAndSaysWhere()
    {
        // PostgreSQL marks the transaction aborted, so every later row would fail for a reason
        // that has nothing to do with itself. Reporting those would be worse than admitting the
        // run stopped.
        mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new DbUpdateException("unique violation"));

        var report = await Import(TwoRows);

        Assert.Contains(report.Errors, i => i.Line == 2);
        Assert.Contains(report.Errors, i => i.Line == 0 && i.Message.Contains("stopped at line 2"));
        await mediator.Received(1).Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, discards);
    }

    [Fact]
    public async Task UnimportableEntity_IsRefusedByName()
    {
        var report = await Sut().ImportAsync(new MasterDataImportRequest("Sprocket", TwoRows));

        Assert.False(report.Succeeded);
        Assert.Contains("Sprocket", Assert.Single(report.Errors).Message);
    }

    [Fact]
    public async Task EntityTypeIsMatchedCaseInsensitively()
    {
        PipelineReturns(Ok());

        var report = await Sut().ImportAsync(new MasterDataImportRequest("widget", TwoRows));

        Assert.True(report.Succeeded);
    }

    [Fact]
    public void Template_CarriesTheHeadersAndASampleRow()
    {
        var template = Sut().BuildTemplate("Widget");

        Assert.Equal("Code,Name\nPHMNL,Manila\n", template);
    }

    [Fact]
    public void Template_IsNullForAnEntityThatCannotBeImported()
    {
        Assert.Null(Sut().BuildTemplate("Sprocket"));
    }

    /// <summary>
    /// Stands in for a real entity's row importer. The orchestrator is what is under test here -
    /// the per-entity half is two columns and no lookups on purpose.
    /// </summary>
    private sealed class FakeRowImporter : IMasterDataRowImporter
    {
        public Func<MasterDataImportRow, bool> Reject { get; set; } = _ => false;

        public MasterDataImportOverride LastOverride { get; private set; } = MasterDataImportOverride.None;

        public string EntityType => "Widget";

        public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
        [
            new("Code", false, "The code.", "PHMNL"),
            new("Name", true, "The name.", "Manila")
        ];

        public Task<MasterDataRowBinding> BindAsync(
            MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
        {
            LastOverride = overrides;

            return Task.FromResult(Reject(row)
                ? MasterDataRowBinding.Rejected(MasterDataImportIssue.Row(row.Line, "Unreadable."))
                : MasterDataRowBinding.For(new object()));
        }
    }
}
