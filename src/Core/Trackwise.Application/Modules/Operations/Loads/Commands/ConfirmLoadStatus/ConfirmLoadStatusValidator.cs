using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class ConfirmLoadStatusValidator : AbstractValidator<ConfirmLoadStatusCommand>
{
    public ConfirmLoadStatusValidator()
    {
        RuleFor(i => i.LoadId).NotEmpty();
        RuleFor(i => i.LoadStatus).NotNull();
    }
}
