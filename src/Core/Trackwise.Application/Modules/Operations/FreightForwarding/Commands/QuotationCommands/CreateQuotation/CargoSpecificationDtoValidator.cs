using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

public class CargoSpecificationDtoValidator : AbstractValidator<CargoSpecificationDto>
{
    public CargoSpecificationDtoValidator()
    {
        RuleFor(x => x.Commodity)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TotalWeightKg)
            .GreaterThan(0)
            .WithMessage("Total cargo weight must be greater than 0 kg.");

        RuleFor(x => x.PalletCount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.DeclaredCargoValueAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.DeclaredCargoValueCurrency)
            .NotEmpty()
            .Length(3)
            .When(x => x.DeclaredCargoValueAmount > 0)
            .WithMessage("Currency code is required when declared cargo value is provided.");
    }
}
