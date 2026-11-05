using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Behaviours;

/// <summary>
/// Runs the dependency probe ahead of any handler that retires a master record, so every path that
/// can deactivate or delete one inherits the check - UI, API, bulk import and ETL alike.
/// <para>
/// The counterpart to <see cref="MasterDataWriteBehaviour{TRequest,TResponse}" />: that one stops a
/// duplicate arriving, this one stops a record leaving while things still point at it. Both live in
/// the pipeline rather than in handlers for the same reason the audit lives in an interceptor - a
/// caller written next year gets the behaviour without knowing it exists.
/// </para>
/// </summary>
public sealed class MasterDataDeactivateBehaviour<TRequest, TResponse>(
    IDependencyProbe probe,
    IMasterDataPolicyProvider policies,
    ILogger<MasterDataDeactivateBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IMasterDataDeactivateCommand
    where TResponse : IResult, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // An ordinary edit is not a retirement. Checked before the policy lookup because this is
        // the common case - most updates leave the record exactly as available as it was.
        if (request.Deactivation is MasterDataDeactivation.None)
        {
            return await next(cancellationToken);
        }

        var policy = policies.Find(request.MasterDataEntityType);

        if (policy is null)
        {
            // Not governed by the module - nothing to enforce.
            return await next(cancellationToken);
        }

        var result = await probe.ProbeAsync(
            new DependencyProbeRequest(
                request.MasterDataEntityType,
                request.MasterDataRecordId,
                request.Deactivation),
            cancellationToken);

        if (!result.IsBlocked)
        {
            return await next(cancellationToken);
        }

        if (!request.ConfirmedDespiteDependents)
        {
            // Returned rather than thrown, matching the duplicate gate: IResult exposes init-only
            // Error and ErrorCode, so a behaviour short-circuits with a typed failure the way a
            // handler does. Clients branch on the code and fetch the structured groups separately.
            return new TResponse
            {
                Error = DependentRecordMessage.For(
                    request.MasterDataEntityType, request.Deactivation, result.Blocking),
                ErrorCode = ErrorCodes.MasterDataRecordInUse
            };
        }

        // Retiring something in use is often the right call, so this is logged rather than refused -
        // but it is the line an investigation starts from when the dependents turn out to matter.
        logger.LogInformation(
            "Master data override: {EntityType} {RecordId} {Intent} despite {Count} dependent record(s). Reason: {Reason}",
            request.MasterDataEntityType,
            request.MasterDataRecordId,
            request.Deactivation,
            result.Blocking.Sum(d => d.Count),
            request.DependencyReason ?? "(none given)");

        return await next(cancellationToken);
    }
}
