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
/// The gate on the way out, mirroring <see cref="MasterDataWriteBehaviourTests"/> on the way in.
/// These pin that an ordinary edit costs nothing, that a retirement cannot slip past the check, and
/// that advisory dependents do not block - a probe that refuses everything gets overridden by
/// reflex, which is the same as not having one.
/// </summary>
public class MasterDataDeactivateBehaviourTests
{
    private readonly IDependencyProbe probe = Substitute.For<IDependencyProbe>();
    private readonly IMasterDataPolicyProvider policies = new MasterDataPolicyProvider();

    private MasterDataDeactivateBehaviour<FakeDeactivateCommand, Result> Sut() =>
        new(probe, policies,
            NullLogger<MasterDataDeactivateBehaviour<FakeDeactivateCommand, Result>>.Instance);

    private void ProbeReturns(params DependentGroup[] dependents) =>
        probe.ProbeAsync(Arg.Any<DependencyProbeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DependencyProbeResult(dependents));

    [Fact]
    public async Task NoDependents_CallsTheHandler()
    {
        ProbeReturns();
        var called = false;

        var result = await Sut().Handle(new FakeDeactivateCommand(), Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BlockingDependents_StopTheHandlerRunning()
    {
        ProbeReturns(new DependentGroup("trucks", 12, Blocks: true));
        var called = false;

        var result = await Sut().Handle(new FakeDeactivateCommand(), Next(() => called = true), default);

        // Once the handler has run the flag is already cleared, and the dependents are stranded.
        Assert.False(called);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.MasterDataRecordInUse, result.ErrorCode);
    }

    [Fact]
    public async Task BlockedMessage_NamesTheCountsSoTheStewardCanSizeTheWork()
    {
        ProbeReturns(
            new DependentGroup("trucks", 12, Blocks: true),
            new DependentGroup("open loads", 3, Blocks: true));

        var result = await Sut().Handle(new FakeDeactivateCommand(), Next(), default);

        Assert.Contains("12 trucks", result.Error);
        Assert.Contains("3 open loads", result.Error);
    }

    [Fact]
    public async Task DeleteAndDeactivate_ExplainThemselvesDifferently()
    {
        ProbeReturns(new DependentGroup("trucks", 12, Blocks: true));

        var deactivate = await Sut().Handle(
            new FakeDeactivateCommand { Deactivation = MasterDataDeactivation.Deactivate }, Next(), default);

        var delete = await Sut().Handle(
            new FakeDeactivateCommand { Deactivation = MasterDataDeactivation.Delete }, Next(), default);

        // Deactivating something in use is often right; deleting it contradicts what delete means.
        Assert.Contains("stops being offered", deactivate.Error);
        Assert.Contains("created by mistake", delete.Error);
    }

    [Fact]
    public async Task AdvisoryDependents_DoNotBlock()
    {
        // Counted and worth reporting, but not an obstacle - historical documents keep resolving
        // to a retired record perfectly well.
        ProbeReturns(new DependentGroup("archived documents", 400, Blocks: false));
        var called = false;

        var result = await Sut().Handle(new FakeDeactivateCommand(), Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ZeroCount_DoesNotBlockEvenOnABlockingRule()
    {
        ProbeReturns(new DependentGroup("trucks", 0, Blocks: true));
        var called = false;

        var result = await Sut().Handle(new FakeDeactivateCommand(), Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Confirmed_ProceedsDespiteDependents()
    {
        ProbeReturns(new DependentGroup("trucks", 12, Blocks: true));
        var called = false;

        var command = new FakeDeactivateCommand
        {
            ConfirmedDespiteDependents = true,
            DependencyReason = "Office closed; trucks move to Cebu next week."
        };

        var result = await Sut().Handle(command, Next(() => called = true), default);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task OrdinaryEdit_SkipsTheProbeEntirely()
    {
        // The common case. An update that leaves the record available must not pay for a query.
        var command = new FakeDeactivateCommand { Deactivation = MasterDataDeactivation.None };

        var result = await Sut().Handle(command, Next(), default);

        Assert.True(result.IsSuccess);
        await probe.DidNotReceive().ProbeAsync(Arg.Any<DependencyProbeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UngovernedEntity_SkipsTheProbeEntirely()
    {
        var command = new FakeDeactivateCommand { MasterDataEntityType = "Spaceship" };

        await Sut().Handle(command, Next(), default);

        await probe.DidNotReceive().ProbeAsync(Arg.Any<DependencyProbeRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Probe_IsToldWhichRecordAndWhichIntent()
    {
        ProbeReturns();
        var command = new FakeDeactivateCommand { Deactivation = MasterDataDeactivation.Delete };

        await Sut().Handle(command, Next(), default);

        // Delete is held to a stricter standard than deactivate, so the intent has to reach the
        // probe rather than being flattened to "retiring".
        await probe.Received(1).ProbeAsync(
            Arg.Is<DependencyProbeRequest>(r =>
                r.EntityType == "Branch"
                && r.RecordId == command.MasterDataRecordId
                && r.Intent == MasterDataDeactivation.Delete),
            Arg.Any<CancellationToken>());
    }

    private static RequestHandlerDelegate<Result> Next(Action? onCalled = null) =>
        _ =>
        {
            onCalled?.Invoke();
            return Task.FromResult(Result.Ok());
        };

    private sealed class FakeDeactivateCommand : IRequest<Result>, IMasterDataDeactivateCommand
    {
        public string MasterDataEntityType { get; init; } = "Branch";
        public Guid MasterDataRecordId { get; } = Guid.NewGuid();
        public MasterDataDeactivation Deactivation { get; init; } = MasterDataDeactivation.Deactivate;
        public bool ConfirmedDespiteDependents { get; init; }
        public string? DependencyReason { get; init; }
    }
}
