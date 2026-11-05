using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class ImportMasterDataValidator : AbstractValidator<ImportMasterDataCommand>
{
    /// <summary>
    /// Refuses a file far past what the row cap allows before it is parsed, since the parse is what
    /// would cost. Generous per row - port names and addresses are long.
    /// </summary>
    private const int MaxContentLength = 4 * 1024 * 1024;

    public ImportMasterDataValidator()
    {
        RuleFor(i => i.EntityType).NotEmpty();

        RuleFor(i => i.Content)
            .NotEmpty()
            .MaximumLength(MaxContentLength)
            .WithMessage("The file is too large to import in one go. Split it.");

        // Same bargain as a single record's duplicate override, and for the same reason: months
        // later the reason is what tells a reviewer whether the call was sound. A blanket override
        // across a whole file makes that more important, not less.
        RuleFor(i => i.DuplicateReason)
            .NotEmpty()
            .WithMessage("Give a reason for importing rows that match existing records.")
            .MaximumLength(500)
            .When(i => i.AcceptDuplicates);
    }
}
