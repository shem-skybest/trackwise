using Trackwise.Application.Modules.Integrations.AICopilot.Commands;
using Trackwise.Application.Modules.Integrations.AIDispatch.Commands;
using Trackwise.Application.Tests.TestKit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Agents;

/// <summary>
/// These handlers only delegate, so the one thing left to get wrong per surface is the scope they
/// pass. The behaviour behind it lives in <see cref="AgentConversationCommandsTests"/>.
/// </summary>
public class AgentSurfaceScopeTests
{
    private readonly AgentTestContext ctx = new();

    [Fact]
    public async Task CreateDispatchConversation_UsesDispatchKind()
    {
        AgentConversation? added = null;
        await ctx.ConversationRepo.AddAsync(
            Arg.Do<AgentConversation>(c => added = c), Arg.Any<CancellationToken>());

        await new CreateAIDispatchConversationHandler(ctx.Commands, ctx.CurrentUser)
            .Handle(new CreateAIDispatchConversationCommand(), CancellationToken.None);

        Assert.Equal(AgentConversationKind.Dispatch, added!.Kind);
    }

    [Fact]
    public async Task CreateCopilotConversation_UsesCopilotKind()
    {
        AgentConversation? added = null;
        await ctx.ConversationRepo.AddAsync(
            Arg.Do<AgentConversation>(c => added = c), Arg.Any<CancellationToken>());

        await new CreateAICopilotConversationHandler(ctx.Commands, ctx.CurrentUser)
            .Handle(new CreateAICopilotConversationCommand(), CancellationToken.None);

        Assert.Equal(AgentConversationKind.Copilot, added!.Kind);
    }

    [Fact]
    public async Task RenameDispatchConversation_RejectsACopilotConversation()
    {
        var conversation = ctx.SetConversation(kind: AgentConversationKind.Copilot);

        var result = await new RenameAIDispatchConversationHandler(ctx.Commands).Handle(
            new RenameAIDispatchConversationCommand { ConversationId = conversation.Id, Title = "x" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RenameCopilotConversation_RejectsAnotherUsersConversation()
    {
        var conversation = ctx.SetConversation(
            createdById: Guid.NewGuid(), kind: AgentConversationKind.Copilot);

        var result = await new RenameAICopilotConversationHandler(ctx.Commands, ctx.CurrentUser).Handle(
            new RenameAICopilotConversationCommand { ConversationId = conversation.Id, Title = "x" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    /// <summary>Tenant-shared: a dispatch conversation someone else opened is still deletable.</summary>
    [Fact]
    public async Task DeleteDispatchConversation_AllowsAnotherUsersConversation()
    {
        var conversation = ctx.SetConversation(
            createdById: Guid.NewGuid(), kind: AgentConversationKind.Dispatch);

        var result = await new DeleteAIDispatchConversationHandler(ctx.Commands).Handle(
            new DeleteAIDispatchConversationCommand { ConversationId = conversation.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteCopilotConversation_RejectsAnotherUsersConversation()
    {
        var conversation = ctx.SetConversation(
            createdById: Guid.NewGuid(), kind: AgentConversationKind.Copilot);

        var result = await new DeleteAICopilotConversationHandler(ctx.Commands, ctx.CurrentUser).Handle(
            new DeleteAICopilotConversationCommand { ConversationId = conversation.Id },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ctx.ConversationRepo.DidNotReceiveWithAnyArgs().Delete(default);
    }
}
