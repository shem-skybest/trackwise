using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Commands;

[RequiresFeature(TenantFeature.AIRateNegotiation)]
public class DeleteLaneRateFloorCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
}
