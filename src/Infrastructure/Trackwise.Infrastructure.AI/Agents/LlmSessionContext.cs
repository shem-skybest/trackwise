using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.AI.Llm.Contracts;
using Trackwise.Infrastructure.AI.Llm;

namespace Trackwise.Infrastructure.AI.Agents;

/// <summary>Everything both conversation builders need before they can shape their own prompt.</summary>
internal sealed record LlmSessionContext(
    Tenant Tenant,
    ILlmProvider Provider,
    LlmModelSelection Selection,
    IReadOnlySet<TenantFeature> EnabledFeatures,
    ReasoningEffort Effort);
