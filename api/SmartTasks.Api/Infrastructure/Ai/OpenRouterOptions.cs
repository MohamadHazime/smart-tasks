using System.ComponentModel.DataAnnotations;

namespace SmartTasks.Api.Infrastructure.Ai;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    [Required, Url]
    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1/";

    [Required]
    public string Model { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    [Range(5, 120)]
    public int TimeoutSeconds { get; init; } = 30;
}
