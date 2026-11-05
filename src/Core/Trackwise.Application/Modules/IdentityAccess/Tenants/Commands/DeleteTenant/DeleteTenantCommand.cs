using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Commands;

public class DeleteTenantCommand : ICommand
{
    public Guid Id { get; set; }
}
