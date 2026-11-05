using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class ConditionReportMapper
{
    public static ConditionDefectDto ToDto(this ConditionDefect entity)
    {
        return new ConditionDefectDto
        {
            Id = entity.Id,
            PartCategory = entity.PartCategory,
            PartCategoryDisplay = entity.PartCategory.GetDescription(),
            Description = entity.Description,
            Severity = entity.Severity,
            SeverityDisplay = entity.Severity.GetDescription()
        };
    }
}
