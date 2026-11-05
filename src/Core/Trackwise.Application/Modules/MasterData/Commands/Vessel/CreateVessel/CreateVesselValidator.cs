using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateVesselValidator : AbstractValidator<CreateVesselCommand>
{
    public CreateVesselValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Type).IsInEnum();

        RuleFor(i => i.CallSign).MaximumLength(20);
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Checked on the normalised value, because that is what gets stored: a steward pasting
        // "IMO 9395044" off a manifest is entering a valid number, and rejecting it for the prefix
        // would teach them to strip it by hand every time.
        RuleFor(i => i.ImoNumber)
            .Must(value => Domain.Entities.Vessel.NormalizeImoNumber(value)?.Length == 7)
            .WithMessage("An IMO number is seven digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.ImoNumber));

        RuleFor(i => i.FlagCountryCode).Length(2)
            .WithMessage("FlagCountryCode must be a 2-letter ISO 3166-1 code.")
            .When(i => !string.IsNullOrWhiteSpace(i.FlagCountryCode));

        // Zero would read as "this ship carries no containers", which is what null already says
        // properly for a tanker.
        RuleFor(i => i.TeuCapacity).GreaterThan(0)
            .WithMessage("Leave TEU capacity empty rather than zero when it does not apply.")
            .When(i => i.TeuCapacity.HasValue);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing vessels.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
