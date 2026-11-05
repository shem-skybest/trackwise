using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.ApiKeys.Queries;

public sealed class GetApiKeysQuery : IQuery<Result<List<ApiKeyDto>>>;
