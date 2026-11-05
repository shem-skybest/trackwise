using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

[RequiresFeature(TenantFeature.Workfiles)]
public class GetBusinessPartnerByIdQuery : IQuery<Result<BusinessPartnerDto>>, IHaveId
{
    public Guid Id { get; set; }
}
