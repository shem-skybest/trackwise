using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Commands;

public class UpdateCustomerCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? Address { get; set; }
    public CustomerStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? TaxId { get; set; }
    public bool IsVatExempt { get; set; }
    public BillingCycle? BillingCycle { get; set; }
}
