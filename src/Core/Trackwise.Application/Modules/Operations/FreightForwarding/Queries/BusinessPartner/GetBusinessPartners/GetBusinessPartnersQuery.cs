using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

[RequiresFeature(TenantFeature.Workfiles)]
public class GetBusinessPartnersQuery : SearchableQuery, IQuery<PagedResult<BusinessPartnerDto>>
{
    /// <summary>
    /// Restricts results to partners that can act in this capacity - what the workfile header's
    /// shipping-line / trucker / broker pickers filter on.
    /// </summary>
    public BusinessPartnerRole? Role { get; set; }

    public bool? IsActive { get; set; }
}
