using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class AIDispatchDecisionMapper
{
    public static AgentDecisionDto ToDto(this AgentDecision entity)
    {
        var dto = entity.ToDtoCore();
        dto.ToolResult = AgentToolResultJson.Deserialize(entity.ToolOutput);
        return dto;
    }

    [MapperIgnoreSource(nameof(AgentDecision.Session))]
    [MapperIgnoreSource(nameof(AgentDecision.ToolOutput))]
    [MapperIgnoreTarget(nameof(AgentDecisionDto.LoadName))]
    [MapperIgnoreTarget(nameof(AgentDecisionDto.TruckNumber))]
    [MapperIgnoreTarget(nameof(AgentDecisionDto.ToolResult))]
    private static partial AgentDecisionDto ToDtoCore(this AgentDecision entity);
}
