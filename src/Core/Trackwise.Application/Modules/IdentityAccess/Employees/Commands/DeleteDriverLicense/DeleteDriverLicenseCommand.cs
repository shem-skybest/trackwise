using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

public class DeleteDriverLicenseCommand : ICommand
{
    public Guid LicenseId { get; set; }
}
