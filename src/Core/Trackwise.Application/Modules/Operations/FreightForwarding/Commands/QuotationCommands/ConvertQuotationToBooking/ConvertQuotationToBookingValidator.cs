using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ConvertQuotationToBooking;

internal sealed class ConvertQuotationToBookingValidator : AbstractValidator<ConvertQuotationToBookingCommand>
{
    public ConvertQuotationToBookingValidator()
    {
        RuleFor(i => i.QuotationId).NotEmpty();

        When(i => i.RequestedPickupDate.HasValue && i.RequestedDeliveryDate.HasValue, () =>
        {
            RuleFor(i => i.RequestedDeliveryDate!.Value)
                .GreaterThanOrEqualTo(i => i.RequestedPickupDate!.Value)
                .WithMessage("Requested delivery date must be on or after requested pickup date.");
        });
    }
}
