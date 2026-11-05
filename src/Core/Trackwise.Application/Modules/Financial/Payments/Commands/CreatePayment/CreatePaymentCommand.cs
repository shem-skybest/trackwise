using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

public class CreatePaymentCommand : ICommand
{
    public string? StripePaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public Address? BillingAddress { get; set; }
    public string? Description { get; set; }
}
