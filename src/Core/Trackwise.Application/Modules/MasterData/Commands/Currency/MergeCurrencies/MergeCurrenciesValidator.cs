using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeCurrenciesValidator : AbstractValidator<MergeCurrenciesCommand>
{
    public MergeCurrenciesValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A currency cannot be merged into itself.");

        RuleFor(i => i.Reason).MaximumLength(500);
    }
}
