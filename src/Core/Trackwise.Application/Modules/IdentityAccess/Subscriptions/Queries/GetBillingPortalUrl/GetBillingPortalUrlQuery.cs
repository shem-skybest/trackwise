using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

public class GetBillingPortalUrlQuery : IQuery<Result<BillingPortalUrlDto>>
{
    public required string ReturnUrl { get; set; }
}
