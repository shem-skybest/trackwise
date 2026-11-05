using System.ComponentModel;
using Trackwise.Application.Abstractions.Agents;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using MediatR;

namespace Trackwise.Infrastructure.AI.Tools.Dispatch;

internal sealed class DispatchTripTool(IMediator mediator)
    : AgentTool<DispatchTripTool.Input>, IAgentToolMetadata
{
    internal sealed record Input
    {
        [Description("The trip ID (GUID) to dispatch")]
        [AgentEntityId(AgentEntityKind.Trip)]
        public required Guid TripId { get; init; }
    }

    public static AgentToolDefinition Definition => new(
        "dispatch_trip",
        "Dispatch a trip, transitioning it from Draft to Dispatched status. This notifies the driver and starts the trip.")
    {
        RequiredPermission = Permission.Dispatch.Manage,
        DecisionType = AgentDecisionType.DispatchTrip,
        Surfaces = AgentSurfaces.All
    };

    protected override async Task<string> ExecuteAsync(Input input, CancellationToken ct)
    {
        var result = await mediator.Send(new DispatchTripCommand { TripId = input.TripId }, ct);

        return ToolResult.Written(result, new
        {
            success = true,
            trip_id = input.TripId,
            warnings = result.Value?.Warnings ?? []
        });
    }
}
