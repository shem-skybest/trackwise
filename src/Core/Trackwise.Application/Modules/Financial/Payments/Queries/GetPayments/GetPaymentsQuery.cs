using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Queries;

public class GetPaymentsQuery : PagedIntervalQuery, IQuery<PagedResult<PaymentDto>>
{
}
