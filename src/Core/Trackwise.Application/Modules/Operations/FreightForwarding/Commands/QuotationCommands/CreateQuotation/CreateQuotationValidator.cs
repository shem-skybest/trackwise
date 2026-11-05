using FluentValidation;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

public class CreateQuotationValidator : AbstractValidator<CreateQuotationCommand>
{
    public CreateQuotationValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a valid 3-letter ISO code.");

        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.FreightMode).IsInEnum();
        RuleFor(x => x.ShipmentType).IsInEnum();
        RuleFor(x => x.Incoterm).IsInEnum();

        RuleFor(x => x.Lane)
            .NotNull()
            .WithMessage("Lane routing information is required.")
            .SetValidator(new LaneTransitDtoValidator());

        When(x => x.Cargo != null && !IsCargoEmpty(x.Cargo), () =>
        {
            RuleFor(x => x.Cargo!)
                .SetValidator(new CargoSpecificationDtoValidator());
        });
    }

    private static bool IsCargoEmpty(CargoSpecificationDto cargo)
    {
        return string.IsNullOrWhiteSpace(cargo.Commodity)
            && cargo.TotalWeightKg <= 0
            && cargo.PalletCount <= 0
            && cargo.DeclaredCargoValueAmount <= 0;
    }
}
