using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartTasks.Api.Application.Ai;

namespace SmartTasks.Api.Infrastructure.Ai;

public sealed class OpenRouterService(
    HttpClient http,
    IOptions<OpenRouterOptions> options,
    ILogger<OpenRouterService> logger) : IAiService
{
    private static readonly string SystemPrompt =
        "You break a task down into small, actionable subtasks. " +
        $"Respond with ONLY a JSON array of {SubTaskSuggestionParser.MinSuggestions} to {SubTaskSuggestionParser.MaxSuggestions} " +
        "short subtask titles as strings, each under 100 characters. " +
        "No prose, no markdown, no numbering, no code fences.";

    public async Task<IReadOnlyList<string>> SuggestSubTasksAsync(
        string title,
        string? description,
        IReadOnlyCollection<string> existingSubTasks,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new AiServiceException("The OpenRouter API key is not configured.");

        var request = new ChatCompletionRequest(
            settings.Model,
            [
                new ChatMessage("system", SystemPrompt),
                new ChatMessage("user", BuildUserPrompt(title, description, existingSubTasks))
            ],
            Temperature: 0.3,
            MaxTokens: 400);

        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new("Bearer", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceException("The AI provider timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException("Could not reach the AI provider.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("OpenRouter returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new AiServiceException(DescribeFailure(response.StatusCode));
            }

            ChatCompletionResponse? completion;
            try
            {
                completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new AiServiceException("The AI provider returned an unreadable response.", ex);
            }

            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            return SubTaskSuggestionParser.Parse(content);
        }
    }

    private static string BuildUserPrompt(string title, string? description, IReadOnlyCollection<string> existing) =>
        $"""
        Task: {title}
        Description: {(string.IsNullOrWhiteSpace(description) ? "(none)" : description)}
        Existing subtasks (do not repeat these): {(existing.Count == 0 ? "(none)" : string.Join("; ", existing))}
        """;

    private static string DescribeFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "The AI provider rejected the API key.",
        HttpStatusCode.PaymentRequired => "The AI provider account has insufficient credits.",
        HttpStatusCode.TooManyRequests => "The AI provider rate limit was reached. Try again shortly.",
        _ => $"The AI provider returned an error ({(int)status})."
    };
}
