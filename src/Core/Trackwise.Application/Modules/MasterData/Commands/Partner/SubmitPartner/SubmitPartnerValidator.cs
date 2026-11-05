using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class SubmitPartnerValidator : AbstractValidator<SubmitPartnerCommand>
{
    public SubmitPartnerValidator()
    {
        RuleFor(i => i.Id).NotEmpty();
        RuleFor(i => i.Note).MaximumLength(1000);
    }
}
