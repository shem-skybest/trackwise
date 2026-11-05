using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Commands;

public class ImpersonateUserCommand : ICommand<Result<ImpersonateUserResult>>
{
    public required string TargetEmail { get; set; }
    public required string MasterPassword { get; set; }
}
