using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public class CustomerDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? Address { get; set; }
    public CustomerStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? TaxId { get; set; }
    public bool IsVatExempt { get; set; }
    public BillingCycle? BillingCycle { get; set; }
    public DateTime CreatedAt { get; set; }
}
