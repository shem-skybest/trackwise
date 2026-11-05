using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AddQuotationLine;

internal sealed class AddQuotationLineValidator : AbstractValidator<AddQuotationLineCommand>
{
    public AddQuotationLineValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
        RuleFor(x => x.TariffRateId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.MarkupType).IsInEnum();
        RuleFor(x => x.ServiceType).IsInEnum();

        When(x => x.MarkupType == MarkupType.Percentage, () =>
        {
            RuleFor(x => x.MarkupValue)
                .GreaterThanOrEqualTo(-100m)
                .WithMessage("Percentage markup cannot discount more than 100%.");
        });

        When(x => x.FxRate.HasValue, () =>
        {
            RuleFor(x => x.FxRate!.Value).GreaterThan(0).WithMessage("FxRate must be positive.");
        });

        When(x => x.BaseAmount.HasValue, () =>
        {
            RuleFor(x => x.BaseAmount!.Value).GreaterThan(0).WithMessage("BaseAmount must be greater than zero.");
        });
    }
}
