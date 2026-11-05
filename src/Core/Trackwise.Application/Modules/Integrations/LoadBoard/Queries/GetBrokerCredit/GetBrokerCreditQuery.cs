using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Queries;

[RequiresFeature(TenantFeature.LoadBoard)]
public class GetBrokerCreditQuery : IQuery<Result<BrokerCreditDto>>
{
    public required string McNumber { get; set; }

    /// <summary>
    /// Optional listing to stamp with the fetched credit data.
    /// </summary>
    public Guid? ListingId { get; set; }
}
