using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.ApiKeys.Commands;

public sealed class RevokeApiKeyCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
