using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Behaviours;

/// <summary>
/// Runs the master-data write pipeline's duplicate-probe and policy stages ahead of the handler, so
/// every write path inherits them - UI, API, bulk import and ETL alike.
/// <para>
/// Sits after <see cref="ValidationBehaviour{TRequest,TResponse}" /> deliberately: probing on
/// fields that have not been validated wastes a query and can surface nonsense candidates.
/// </para>
/// <para>
/// The probe runs <b>before</b> the record is saved. Catching a duplicate at the door costs the
/// steward seconds; reconciling one months later costs a week, which is the whole reason this
/// module exists.
/// </para>
/// </summary>
public sealed class MasterDataWriteBehaviour<TRequest, TResponse>(
    IDuplicateProbe probe,
    IMasterDataPolicyProvider policies,
    ILogger<MasterDataWriteBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IMasterDataWriteCommand
    where TResponse : IResult, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var policy = policies.Find(request.MasterDataEntityType);

        if (policy is null)
        {
            // Not governed by the module - nothing to enforce.
            return await next(cancellationToken);
        }

        var result = await probe.ProbeAsync(
            new DuplicateProbeRequest(
                request.MasterDataEntityType,
                request.DedupeFields,
                request.MasterDataRecordId),
            cancellationToken);

        if (result.IsBlocked)
        {
            if (!request.ConfirmedDistinct)
            {
                // Returned rather than thrown: IResult exposes init-only Error and ErrorCode, so a
                // behaviour can short-circuit with a typed failure the way handlers do. Clients
                // branch on the code; the structured candidate list comes from the probe endpoint.
                return new TResponse
                {
                    Error = DuplicateRecordMessage.For(request.MasterDataEntityType, result.Blocking),
                    ErrorCode = ErrorCodes.DuplicateMasterDataRecord
                };
            }

            // An override is a decision worth being able to find later, so it is logged even when
            // the steward is right. The audit interceptor records the record; this records the why.
            logger.LogInformation(
                "Master data override: {EntityType} created despite {Count} candidate(s) at or above {Threshold}. Reason: {Reason}",
                request.MasterDataEntityType,
                result.Blocking.Count,
                policy.HardThreshold,
                request.DistinctReason ?? "(none given)");
        }

        return await next(cancellationToken);
    }
}
