using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

public class ProcessPaymentCommand : ICommand
{
    public Guid PaymentId { get; set; }
}
