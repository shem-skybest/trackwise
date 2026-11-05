using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Features.Queries;

/// <summary>
/// Query to get the default feature configuration for new tenants.
/// </summary>
public class GetDefaultFeaturesQuery : IQuery<Result<IReadOnlyList<DefaultFeatureStatusDto>>>;
