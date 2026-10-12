using System.Text.Json.Serialization;

namespace SmartTasks.Api.Infrastructure.Ai;

internal sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    double Temperature,
    [property: JsonPropertyName("max_tokens")] int MaxTokens);

internal sealed record ChatMessage(string Role, string? Content);

internal sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice>? Choices);

internal sealed record ChatChoice(ChatMessage? Message);
