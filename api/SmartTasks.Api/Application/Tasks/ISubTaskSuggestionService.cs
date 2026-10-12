namespace SmartTasks.Api.Application.Tasks;

public interface ISubTaskSuggestionService
{
    Task<SubTaskSuggestionsResponse?> SuggestAsync(int taskId, CancellationToken cancellationToken);
}
