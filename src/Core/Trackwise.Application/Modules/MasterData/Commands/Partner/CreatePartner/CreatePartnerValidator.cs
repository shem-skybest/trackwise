using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreatePartnerValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);

        RuleFor(i => i.Roles)
            .NotEmpty().WithMessage("A business partner must have at least one role.");
        RuleForEach(i => i.Roles).IsInEnum();

        RuleFor(i => i.ContactName).MaximumLength(200);
        RuleFor(i => i.Email).EmailAddress().MaximumLength(256).When(i => !string.IsNullOrEmpty(i.Email));
        RuleFor(i => i.Phone).MaximumLength(50);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);

        // Checked on the normalised value, because that is what gets stored: a steward pasting
        // "123-456-789-000" off a BIR certificate is entering a valid TIN, and rejecting it for the
        // dashes would teach them to strip it by hand every time.
        RuleFor(i => i.TaxId)
            .Must(value => Domain.Entities.BusinessPartner.NormalizeTaxId(value) is { Length: <= 50 })
            .WithMessage("A TIN must contain at least one digit, and no more than 50.")
            .When(i => !string.IsNullOrWhiteSpace(i.TaxId));

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing partners.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
