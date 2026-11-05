using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class PromoteBookingToWorkfileValidator : AbstractValidator<PromoteBookingToWorkfileCommand>
{
    public PromoteBookingToWorkfileValidator()
    {
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.ReportCategory)
            .NotNull().WithMessage("Select a report category (PSB or Not PSB) before saving.")
            .IsInEnum();
        RuleFor(i => i.ReferenceNumber).MaximumLength(40);
        RuleFor(i => i.BillOfLadingNumber).MaximumLength(100);
        RuleFor(i => i.VesselName).MaximumLength(200);
        RuleFor(i => i.VoyageNumber).MaximumLength(50);
        RuleFor(i => i.Origin).MaximumLength(200);
        RuleFor(i => i.Destination).MaximumLength(200);
        RuleFor(i => i.Commodity).MaximumLength(500);
        RuleFor(i => i.TaggingCategory).MaximumLength(100);
        RuleFor(i => i.CreditTermDays).GreaterThanOrEqualTo(0).When(i => i.CreditTermDays.HasValue);
        RuleFor(i => i.ExchangeRate).GreaterThan(0).When(i => i.ExchangeRate.HasValue);
    }
}
