using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

/// <summary>Every permission a staff role can hold, grouped and labelled for the role editor.</summary>
public class GetPermissionCatalogQuery : IQuery<Result<PermissionGroupDto[]>>;
