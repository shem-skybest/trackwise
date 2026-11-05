using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class ApprovePartnerValidator : AbstractValidator<ApprovePartnerCommand>
{
    public ApprovePartnerValidator()
    {
        RuleFor(i => i.Id).NotEmpty();
        RuleFor(i => i.Note).MaximumLength(1000);
    }
}
