using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class UpdateWorkfileHeaderValidator : AbstractValidator<UpdateWorkfileHeaderCommand>
{
    public UpdateWorkfileHeaderValidator()
    {
        RuleFor(i => i.BillOfLadingNumber).MaximumLength(100);
        RuleFor(i => i.VesselName).MaximumLength(200);
        RuleFor(i => i.VoyageNumber).MaximumLength(50);
        RuleFor(i => i.Origin).MaximumLength(200);
        RuleFor(i => i.Destination).MaximumLength(200);
        RuleFor(i => i.Commodity).MaximumLength(500);
        RuleFor(i => i.TaggingCategory).MaximumLength(100);
        RuleFor(i => i.CreditTermDays).GreaterThanOrEqualTo(0).When(i => i.CreditTermDays.HasValue);
        RuleFor(i => i.ExchangeRate).GreaterThan(0).When(i => i.ExchangeRate.HasValue);
        RuleFor(i => i.FinalExchangeRate).GreaterThan(0).When(i => i.FinalExchangeRate.HasValue);
    }
}
