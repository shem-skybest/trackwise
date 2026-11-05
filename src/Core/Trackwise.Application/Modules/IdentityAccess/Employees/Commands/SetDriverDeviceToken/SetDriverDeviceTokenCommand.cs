using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

public class SetDriverDeviceTokenCommand : ICommand
{
    public Guid UserId { get; set; }
    public string? DeviceToken { get; set; }
}
