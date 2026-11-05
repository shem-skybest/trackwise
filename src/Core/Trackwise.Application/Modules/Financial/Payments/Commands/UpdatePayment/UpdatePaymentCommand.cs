using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

public class UpdatePaymentCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
    public string? StripePaymentMethodId { get; set; }
    public decimal? Amount { get; set; }
    public PaymentStatus? Status { get; set; }
    public Address? BillingAddress { get; set; }
    public string? Description { get; set; }
}
