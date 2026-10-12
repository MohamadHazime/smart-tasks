using Microsoft.EntityFrameworkCore;
using SmartTasks.Api.Application.Ai;

namespace SmartTasks.Api.Application.Tasks;

public sealed class SubTaskSuggestionService(IAppDbContext db, IAiService ai) : ISubTaskSuggestionService
{
    public async Task<SubTaskSuggestionsResponse?> SuggestAsync(int taskId, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
            return null;

        var existing = task.SubTasks.Select(s => s.Title).ToList();
        var suggestions = await ai.SuggestSubTasksAsync(task.Title, task.Description, existing, cancellationToken);

        var fresh = suggestions
            .Where(s => !existing.Contains(s, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new SubTaskSuggestionsResponse(task.Id, fresh);
    }
}
