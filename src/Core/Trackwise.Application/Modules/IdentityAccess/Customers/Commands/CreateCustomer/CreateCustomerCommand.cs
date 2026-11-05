using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Commands;

public class CreateCustomerCommand : ICommand<Result<CustomerDto>>
{
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? Address { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Notes { get; set; }
    public string? TaxId { get; set; }
    public bool IsVatExempt { get; set; }
    public BillingCycle? BillingCycle { get; set; }
}
