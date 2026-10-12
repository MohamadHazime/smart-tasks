using SmartTasks.Api.Application.Ai;
using SmartTasks.Api.Application.Tasks;
using SmartTasks.Api.Domain;
using SmartTasks.Api.Tests.Infrastructure;

namespace SmartTasks.Api.Tests.Application;

public sealed class SubTaskSuggestionServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SuggestAsync_PassesTaskContextAndFiltersExistingSubTasks()
    {
        var taskId = await SeedTaskAsync("Plan a party", "Saturday evening", "Send invites");
        var ai = new FakeAiService(["send invites", "Book venue", "Order food"]);

        await using var context = _database.CreateContext();
        var result = await new SubTaskSuggestionService(context, ai).SuggestAsync(taskId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(taskId, result.TaskId);
        Assert.Equal(new[] { "Book venue", "Order food" }, result.Suggestions);
        Assert.Equal("Plan a party", ai.ReceivedTitle);
        Assert.Equal("Saturday evening", ai.ReceivedDescription);
        Assert.Equal(new[] { "Send invites" }, ai.ReceivedExisting);
    }

    [Fact]
    public async Task SuggestAsync_DoesNotSaveSuggestions()
    {
        var taskId = await SeedTaskAsync("Plan a party", null);
        var ai = new FakeAiService(["Book venue", "Order food", "Send invites"]);

        await using (var context = _database.CreateContext())
        {
            await new SubTaskSuggestionService(context, ai).SuggestAsync(taskId, CancellationToken.None);
        }

        await using var verify = _database.CreateContext();
        Assert.Empty(verify.SubTasks);
    }

    [Fact]
    public async Task SuggestAsync_WhenTaskMissing_ReturnsNullWithoutCallingAi()
    {
        var ai = new FakeAiService(["Unused", "Unused 2", "Unused 3"]);

        await using var context = _database.CreateContext();
        var result = await new SubTaskSuggestionService(context, ai).SuggestAsync(999, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(ai.ReceivedTitle);
    }

    private async Task<int> SeedTaskAsync(string title, string? description, params string[] subTasks)
    {
        await using var context = _database.CreateContext();
        var task = new TaskItem(title, description, Priority.Medium, null);
        foreach (var subTask in subTasks)
            task.AddSubTask(subTask);

        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task.Id;
    }

    private sealed class FakeAiService(IReadOnlyList<string> suggestions) : IAiService
    {
        public string? ReceivedTitle { get; private set; }
        public string? ReceivedDescription { get; private set; }
        public IReadOnlyCollection<string> ReceivedExisting { get; private set; } = [];

        public Task<IReadOnlyList<string>> SuggestSubTasksAsync(
            string title, string? description, IReadOnlyCollection<string> existingSubTasks, CancellationToken cancellationToken)
        {
            ReceivedTitle = title;
            ReceivedDescription = description;
            ReceivedExisting = existingSubTasks;
            return Task.FromResult(suggestions);
        }
    }
}
