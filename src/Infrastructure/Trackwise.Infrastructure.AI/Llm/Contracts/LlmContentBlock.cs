using System.Text.Json.Nodes;

namespace Trackwise.Infrastructure.AI.Llm.Contracts;

/// <summary>
/// Base type for the polymorphic content blocks that make up an <see cref="LlmMessage"/>.
/// Providers map these to their SDK-native content representations.
/// </summary>
internal abstract record LlmContentBlock;

internal record LlmTextBlock(string Text) : LlmContentBlock;

/// <param name="Signature">
/// Opaque provider signature that must be echoed back with the call (Gemini's thought_signature -
/// Gemini 3 rejects a follow-up turn whose function call lacks it).
/// </param>
internal record LlmToolUseBlock(string Id, string Name, JsonNode? Input, string? Signature = null) : LlmContentBlock;

internal record LlmToolResultBlock(string ToolUseId, string Content) : LlmContentBlock;

/// <summary>An inline document such as a PDF (base64-encoded). Mapped to provider-native document content.</summary>
internal record LlmDocumentBlock(string MediaType, string Base64Data) : LlmContentBlock;

/// <summary>
/// An Anthropic thinking block, kept so the tool loop can echo it back - with thinking on, an
/// assistant turn replayed without it is rejected. Only in-turn replay is required.
/// </summary>
internal record LlmThinkingBlock(string Thinking, string? Signature) : LlmContentBlock;

/// <summary>Safety-redacted reasoning. Opaque, but still has to be echoed back.</summary>
internal record LlmRedactedThinkingBlock(string Data) : LlmContentBlock;
