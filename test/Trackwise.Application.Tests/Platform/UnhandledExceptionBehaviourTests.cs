using System.Data.Common;
using Trackwise.Application.Behaviours;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Trackwise.Application.Tests.Platform;

/// <summary>
/// The driver app shows a 400's message to the crew as-is, so a database error must never come
/// back as its SQL text ("42703: column b.booking_number does not exist").
/// </summary>
public class UnhandledExceptionBehaviourTests
{
    private readonly UnhandledExceptionBehaviour<FakeRequest, Result> sut =
        new(NullLogger<UnhandledExceptionBehaviour<FakeRequest, Result>>.Instance);

    [Fact]
    public async Task Handle_DatabaseError_ReturnsGenericMessageWithServerFaultCode()
    {
        var result = await sut.Handle(new FakeRequest(), Throws(new FakeDbException()), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ServerFault, result.ErrorCode);
        Assert.DoesNotContain("42703", result.Error);
    }

    [Fact]
    public async Task Handle_WrappedDatabaseError_ReturnsServerFaultCode()
    {
        var wrapped = new InvalidOperationException("An error occurred while saving.", new FakeDbException());

        var result = await sut.Handle(new FakeRequest(), Throws(wrapped), default);

        Assert.Equal(ErrorCodes.ServerFault, result.ErrorCode);
    }

    [Fact]
    public async Task Handle_OtherError_KeepsItsMessage()
    {
        var result = await sut.Handle(
            new FakeRequest(), Throws(new InvalidOperationException("The trip is already finished.")), default);

        Assert.Equal("The trip is already finished.", result.Error);
        Assert.Null(result.ErrorCode);
    }

    private static RequestHandlerDelegate<Result> Throws(Exception ex) => _ => throw ex;

    public sealed record FakeRequest : IRequest<Result>;

    private sealed class FakeDbException() : DbException("42703: column b.booking_number does not exist");
}
