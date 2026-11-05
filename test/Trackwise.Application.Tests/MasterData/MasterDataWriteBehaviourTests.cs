using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Behaviours;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The probe has to run <b>before</b> the save, and a blocked write has to carry a code the client
/// can branch on. These pin that the gate cannot be bypassed by accident and cannot be tripped by
/// weak matches - a probe that blocks too eagerly gets switched off by its users.
/// </summary>
public class MasterDataWriteBehaviourTests
{
    private readonly IDuplicateProbe probe = Substitute.For<IDuplicateProbe>();
    private readonly IMasterDataPolicyProvider policies = new MasterDataPolicyProvider();

    private MasterDataWriteBehaviour<FakeWriteCommand, Result> Sut() =>
        new(probe, policies,
            NullLogger<MasterDataWriteBehaviour<FakeWriteCommand, Result>>.Instance);

    private void ProbeReturns(params DuplicateCandidate[] candidates) =>
        probe.ProbeAsync(Arg.Any<DuplicateProbeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DuplicateProbeResult(candidates, HardThreshold: 0.80, SoftThreshold: 0.45));

    private static DuplicateCandidate Candidate(double score, MasterDataState state = MasterDataState.Active) =>
        new(Guid.NewGuid(), "MNL", "Manila Head Office", score, ["name"], state);

    [Fact]
    public async Task NoCandidates_CallsTheHandler()
    {
        ProbeReturns();
        var called = false;

        var result = await Sut().Handle(new FakeWriteCommand(), Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CandidateAboveHardThreshold_BlocksBeforeTheHandlerRuns()
    {
        ProbeReturns(Candidate(0.94));
        var called = false;

        var result = await Sut().Handle(new FakeWriteCommand(), Next(() => called = true), default);

        // Stopping the duplicate at the door is the module's entire economic argument - if the
        // handler ran, the row is already written and the probe was decoration.
        Assert.False(called);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DuplicateMasterDataRecord, result.ErrorCode);
    }

    [Fact]
    public async Task BlockedMessage_NamesTheCandidateSoAnApiClientCanAct()
    {
        ProbeReturns(Candidate(0.94));

        var result = await Sut().Handle(new FakeWriteCommand(), Next(), default);

        Assert.Contains("MNL", result.Error);
        Assert.Contains("Manila Head Office", result.Error);
    }

    [Fact]
    public async Task BlockedMessage_SaysWhenTheMatchIsDeleted()
    {
        ProbeReturns(Candidate(0.94, MasterDataState.Deleted));

        var result = await Sut().Handle(new FakeWriteCommand(), Next(), default);

        // A deleted match means "restore that one", not "pick another name".
        Assert.Contains("deleted", result.Error);
    }

    [Fact]
    public async Task CandidateBelowHardThreshold_DoesNotBlock()
    {
        // Above soft, below hard: worth showing, not worth refusing. A probe that blocks on weak
        // matches trains stewards to override everything.
        ProbeReturns(Candidate(0.60));
        var called = false;

        var result = await Sut().Handle(new FakeWriteCommand(), Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ConfirmedDistinct_ProceedsDespiteAHardMatch()
    {
        ProbeReturns(Candidate(0.94));
        var called = false;

        var command = new FakeWriteCommand { ConfirmedDistinct = true, DistinctReason = "Satellite office." };
        var result = await Sut().Handle(command, Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UngovernedEntity_SkipsTheProbeEntirely()
    {
        var command = new FakeWriteCommand { MasterDataEntityType = "Spaceship" };

        await Sut().Handle(command, Next(), default);

        await probe.DidNotReceive().ProbeAsync(Arg.Any<DuplicateProbeRequest>(), Arg.Any<CancellationToken>());
    }

    private static RequestHandlerDelegate<Result> Next(Action? onCalled = null) =>
        _ =>
        {
            onCalled?.Invoke();
            return Task.FromResult(Result.Ok());
        };

    private sealed class FakeWriteCommand : IRequest<Result>, IMasterDataWriteCommand
    {
        public string MasterDataEntityType { get; init; } = "Branch";
        public Guid? MasterDataRecordId => null;
        public IReadOnlyDictionary<string, string?> DedupeFields { get; } =
            new Dictionary<string, string?> { ["Name"] = "Manila Head Ofice" };
        public bool ConfirmedDistinct { get; init; }
        public string? DistinctReason { get; init; }
    }
}
