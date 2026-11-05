using System.ComponentModel;
using Trackwise.Application.Abstractions.Agents;
using Trackwise.Application.Modules.Operations.Loads.Commands;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Policies;
using MediatR;

namespace Trackwise.Infrastructure.AI.Tools.Dispatch;

internal sealed class AssignLoadToTruckTool(IMediator mediator)
    : AgentTool<AssignLoadToTruckTool.Input>, IAgentToolMetadata
{
    internal sealed record Input
    {
        [Description("The load ID (GUID) to assign")]
        [AgentEntityId(AgentEntityKind.Load)]
        public required Guid LoadId { get; init; }

        [Description("The truck ID (GUID) to assign the load to")]
        [AgentEntityId(AgentEntityKind.Truck)]
        public required Guid TruckId { get; init; }

        [Description("Brief explanation of why this assignment is optimal")]
        public required string Reasoning { get; init; }
    }

    public static AgentToolDefinition Definition => new(
        "assign_load_to_truck",
        "Assign a specific load to a specific truck.")
    {
        RequiredPermission = Permission.Dispatch.Manage,
        DecisionType = AgentDecisionType.AssignLoad,
        Surfaces = AgentSurfaces.All
    };

    protected override async Task<string> ExecuteAsync(Input input, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AssignLoadToTruckCommand { LoadId = input.LoadId, TruckId = input.TruckId }, ct);

        return ToolResult.Written(result,
            new { success = true, load_id = input.LoadId, truck_id = input.TruckId });
    }
}
