using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Inspections.Queries;

public class GetInspectionPartCatalogQuery : IQuery<Result<InspectionPartCatalogDto>>
{
    public required LoadType LoadType { get; set; }
}
