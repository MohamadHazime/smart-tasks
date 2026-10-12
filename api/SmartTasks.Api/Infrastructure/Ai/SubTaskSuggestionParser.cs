using System.Text.Json;
using SmartTasks.Api.Application.Ai;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Infrastructure.Ai;

public static class SubTaskSuggestionParser
{
    public const int MinSuggestions = 3;
    public const int MaxSuggestions = 6;

    public static IReadOnlyList<string> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new AiServiceException("The AI returned an empty response.");

        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');
        if (start < 0 || end <= start)
            throw new AiServiceException("The AI response did not contain a JSON array.");

        List<string?>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<string?>>(content[start..(end + 1)]);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException("The AI response was not a valid JSON array of strings.", ex);
        }

        var suggestions = (items ?? [])
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!.Length > SubTask.TitleMaxLength ? s[..SubTask.TitleMaxLength] : s)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSuggestions)
            .ToList();

        if (suggestions.Count < MinSuggestions)
            throw new AiServiceException(
                $"The AI returned {suggestions.Count} usable suggestions; at least {MinSuggestions} are required.");

        return suggestions;
    }
}
