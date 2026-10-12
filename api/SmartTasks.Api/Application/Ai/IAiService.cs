namespace SmartTasks.Api.Application.Ai;

public interface IAiService
{
    Task<IReadOnlyList<string>> SuggestSubTasksAsync(
        string title,
        string? description,
        IReadOnlyCollection<string> existingSubTasks,
        CancellationToken cancellationToken);
}
