using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergePortsValidator : AbstractValidator<MergePortsCommand>
{
    /// <summary>Fields a steward may take from the losing port.</summary>
    private static readonly string[] Mergeable =
        ["Name", "CountryCode", "Type", "Address", "Notes", "BranchId"];

    public MergePortsValidator()
    {
        RuleFor(i => i.SurvivorId).NotEmpty();
        RuleFor(i => i.LoserId).NotEmpty();

        RuleFor(i => i.LoserId)
            .NotEqual(i => i.SurvivorId)
            .WithMessage("A port cannot be merged into itself.");

        // A merge is hard to undo, so the reason is required rather than optional - it is what a
        // reviewer reads when the merge turns out to have been wrong.
        RuleFor(i => i.Reason)
            .NotEmpty().WithMessage("Give a reason for the merge.")
            .MaximumLength(1000);

        RuleForEach(i => i.FieldChoices)
            .Must(choice => Mergeable.Contains(choice.Key))
            .WithMessage(choice => $"'{choice}' is not a field that can be merged.")
            .Must(choice => choice.Value is "survivor" or "loser")
            .WithMessage("Each field choice must be 'survivor' or 'loser'.");
    }
}
