using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateChargeCodeValidator : AbstractValidator<CreateChargeCodeCommand>
{
    public CreateChargeCodeValidator()
    {
        RuleFor(i => i.Code).NotEmpty().MaximumLength(20);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(100);
        RuleFor(i => i.Category).IsInEnum();
        RuleFor(i => i.DefaultUnit).NotEmpty().MaximumLength(30);
        RuleFor(i => i.TaxCode).MaximumLength(50).When(i => i.TaxCode is not null);
    }
}
