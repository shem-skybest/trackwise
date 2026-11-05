using FluentValidation;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class RenameAIDispatchConversationValidator
    : AbstractValidator<RenameAIDispatchConversationCommand>
{
    public RenameAIDispatchConversationValidator()
    {
        // 120 matches the auto-derived title cap in AgentTurnService.DeriveTitle.
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
    }
}
