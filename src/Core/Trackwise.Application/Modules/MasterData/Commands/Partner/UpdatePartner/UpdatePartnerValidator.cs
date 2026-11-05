using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdatePartnerValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);

        RuleFor(i => i.Roles)
            .NotEmpty().WithMessage("A business partner must have at least one role.");
        RuleForEach(i => i.Roles).IsInEnum();

        RuleFor(i => i.ContactName).MaximumLength(200);
        RuleFor(i => i.Email).EmailAddress().MaximumLength(256).When(i => !string.IsNullOrEmpty(i.Email));
        RuleFor(i => i.Phone).MaximumLength(50);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);

        RuleFor(i => i.TaxId)
            .Must(value => Domain.Entities.BusinessPartner.NormalizeTaxId(value) is { Length: <= 50 })
            .WithMessage("A TIN must contain at least one digit, and no more than 50.")
            .When(i => !string.IsNullOrWhiteSpace(i.TaxId));

        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing partners.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);

        // Retiring a partner that open files still name is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a partner that open files still name.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
