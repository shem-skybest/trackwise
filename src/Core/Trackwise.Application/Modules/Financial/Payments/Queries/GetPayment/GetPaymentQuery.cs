using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Queries;

public class GetPaymentQuery : IQuery<Result<PaymentDto>>
{
    public Guid Id { get; set; }
}
