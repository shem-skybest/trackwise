using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RejectPartnerValidator : AbstractValidator<RejectPartnerCommand>
{
    public RejectPartnerValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        RuleFor(i => i.Reason)
            .NotEmpty()
            .WithMessage("Say why the record is going back, or the steward has nothing to act on.")
            .MaximumLength(1000);
    }
}
