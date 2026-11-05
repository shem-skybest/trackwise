using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Commands;

public class DeleteCustomerCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
}
