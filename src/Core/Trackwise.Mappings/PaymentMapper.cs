using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class PaymentMapper
{
    public static PaymentDto ToDto(this Payment entity)
    {
        return new PaymentDto
        {
            Id = entity.Id,
            Amount = entity.Amount,
            CreatedDate = entity.CreatedAt,
            StripePaymentMethodId = entity.StripePaymentMethodId,
            TenantId = entity.TenantId,
            Status = entity.Status,
            Description = entity.Description,
            BillingAddress = entity.BillingAddress
        };
    }
}
