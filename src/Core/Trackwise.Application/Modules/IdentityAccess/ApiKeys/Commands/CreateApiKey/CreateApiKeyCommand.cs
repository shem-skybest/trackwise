using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.ApiKeys.Commands;

public sealed class CreateApiKeyCommand : ICommand<Result<ApiKeyCreatedDto>>
{
    public string Name { get; set; } = string.Empty;
}
