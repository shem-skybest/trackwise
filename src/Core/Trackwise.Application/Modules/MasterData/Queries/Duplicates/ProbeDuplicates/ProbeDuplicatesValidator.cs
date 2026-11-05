using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class ProbeDuplicatesValidator : AbstractValidator<ProbeDuplicatesQuery>
{
    public ProbeDuplicatesValidator()
    {
        RuleFor(i => i.EntityType).NotEmpty().MaximumLength(128);
        RuleFor(i => i.Fields).NotEmpty().WithMessage("Give at least one field to compare.");
    }
}
