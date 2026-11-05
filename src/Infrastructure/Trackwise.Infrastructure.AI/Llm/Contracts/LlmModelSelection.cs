using Trackwise.Application.Abstractions.AI;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Infrastructure.AI.Llm.Contracts;

/// <summary>The resolved model, its provider, and that provider's configuration.</summary>
internal sealed record LlmModelSelection(string Model, LlmProvider Provider, LlmProviderOptions ProviderConfig);
