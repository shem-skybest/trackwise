using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Queries;

internal sealed class GetPaymentHandler : IAppRequestHandler<GetPaymentQuery, Result<PaymentDto>>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public GetPaymentHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result<PaymentDto>> Handle(
        GetPaymentQuery req, CancellationToken ct)
    {
        var paymentEntity = await _tenantUow.Repository<Payment>().GetByIdAsync(req.Id);

        if (paymentEntity is null)
        {
            return Result<PaymentDto>.Fail($"Could not find a payment with ID {req.Id}");
        }

        var paymentDto = paymentEntity.ToDto();
        return Result<PaymentDto>.Ok(paymentDto);
    }
}
