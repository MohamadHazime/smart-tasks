namespace SmartTasks.Api.Domain;

internal static class Guard
{
    public static string RequiredText(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new DomainException($"{field} is required.");
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be at most {maxLength} characters.");
        return trimmed;
    }

    public static string? OptionalText(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be at most {maxLength} characters.");
        return trimmed;
    }
}
