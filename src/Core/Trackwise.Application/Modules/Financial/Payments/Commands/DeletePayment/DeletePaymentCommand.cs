using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

public class DeletePaymentCommand : ICommand, IHaveId
{
    public Guid Id { get; set; }
}
