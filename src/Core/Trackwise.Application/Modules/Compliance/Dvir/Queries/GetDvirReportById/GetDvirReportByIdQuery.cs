using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Queries;

[RequiresFeature(TenantFeature.Dvir)]
public record GetDvirReportByIdQuery(Guid Id) : IQuery<Result<DvirReportDto>>, IHaveId;
