using Trackwise.Application.Abstractions.Agents;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.AI.Llm;

namespace Trackwise.Infrastructure.AI.Llm.Contracts;

/// <summary>
/// Provider-agnostic conversation state. Produced by either conversation builder and consumed by
/// <c>AgentLoopRunner</c>.
/// </summary>
internal record LlmConversation(
    ILlmProvider Provider,
    string SystemPrompt,
    List<LlmMessage> Messages,
    IReadOnlyList<AgentToolDefinition> Tools,
    string Model,
    int MaxTokens,
    ReasoningEffort Effort);
