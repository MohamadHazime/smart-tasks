using Microsoft.EntityFrameworkCore;
using SmartTasks.Api.Application.Tasks;
using SmartTasks.Api.Domain;
using SmartTasks.Api.Tests.Infrastructure;

namespace SmartTasks.Api.Tests.Application;

public sealed class TaskServiceTests : IDisposable
{
    private const int MissingId = 999;

    private readonly SqliteTestDatabase _database = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task CreateAsync_PersistsTaskAndStampsCreatedAt()
    {
        var before = DateTime.UtcNow;

        var created = await CreateTaskAsync("  Write tests  ", Priority.High, "Cover the service");

        Assert.True(created.Id > 0);
        Assert.Equal("Write tests", created.Title);
        Assert.Equal(Priority.High, created.Priority);
        Assert.False(created.IsDone);
        Assert.Null(created.ModifiedAt);
        Assert.InRange(created.CreatedAt, before, DateTime.UtcNow);

        await using var context = _database.CreateContext();
        var stored = await context.Tasks.SingleAsync();
        Assert.Equal("Write tests", stored.Title);
        Assert.Equal("Cover the service", stored.Description);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithBlankTitle_ThrowsDomainException()
    {
        await using var context = _database.CreateContext();
        var service = new TaskService(context);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(new CreateTaskRequest { Title = "   " }, _ct));
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilters_ReturnsOpenFirstThenHighestPriority()
    {
        var low = await CreateTaskAsync("Low open", Priority.Low);
        var high = await CreateTaskAsync("High open", Priority.High);
        var done = await CreateTaskAsync("High done", Priority.High);
        await ToggleAsync(done.Id);

        var tasks = await GetAllAsync(status: null, priority: null);

        Assert.Equal(new[] { high.Id, low.Id, done.Id }, tasks.Select(t => t.Id));
    }

    [Theory]
    [InlineData(TaskStatusFilter.Open, "Open task")]
    [InlineData(TaskStatusFilter.Done, "Done task")]
    public async Task GetAllAsync_FiltersByStatus(TaskStatusFilter status, string expectedTitle)
    {
        await CreateTaskAsync("Open task", Priority.Medium);
        var done = await CreateTaskAsync("Done task", Priority.Medium);
        await ToggleAsync(done.Id);

        var tasks = await GetAllAsync(status, priority: null);

        var task = Assert.Single(tasks);
        Assert.Equal(expectedTitle, task.Title);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPriority()
    {
        await CreateTaskAsync("Low", Priority.Low);
        await CreateTaskAsync("High", Priority.High);

        var tasks = await GetAllAsync(status: null, Priority.High);

        var task = Assert.Single(tasks);
        Assert.Equal("High", task.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTaskWithSubTasks()
    {
        var created = await CreateTaskAsync("Parent", Priority.Medium);
        await AddSubTaskAsync(created.Id, "Child");

        await using var context = _database.CreateContext();
        var task = await new TaskService(context).GetByIdAsync(created.Id, _ct);

        Assert.NotNull(task);
        var subTask = Assert.Single(task.SubTasks);
        Assert.Equal("Child", subTask.Title);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndStampsModifiedAt()
    {
        var created = await CreateTaskAsync("Original", Priority.Low);
        var dueDate = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

        await using var context = _database.CreateContext();
        var updated = await new TaskService(context).UpdateAsync(created.Id, new UpdateTaskRequest
        {
            Title = "Renamed",
            Description = "Now with a description",
            Priority = Priority.High,
            DueDate = dueDate
        }, _ct);

        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated.Title);
        Assert.Equal("Now with a description", updated.Description);
        Assert.Equal(Priority.High, updated.Priority);
        Assert.Equal(dueDate, updated.DueDate);
        Assert.NotNull(updated.ModifiedAt);
    }

    [Fact]
    public async Task ToggleAsync_FlipsIsDoneBackAndForth()
    {
        var created = await CreateTaskAsync("Toggle me", Priority.Medium);

        var first = await ToggleAsync(created.Id);
        var second = await ToggleAsync(created.Id);

        Assert.True(first!.IsDone);
        Assert.NotNull(first.ModifiedAt);
        Assert.False(second!.IsDone);
    }

    [Fact]
    public async Task AddSubTaskAsync_PersistsSubTaskUnderTask()
    {
        var created = await CreateTaskAsync("Parent", Priority.Medium);

        var subTask = await AddSubTaskAsync(created.Id, "  Child  ");

        Assert.NotNull(subTask);
        Assert.Equal("Child", subTask.Title);
        Assert.False(subTask.IsDone);

        await using var context = _database.CreateContext();
        var stored = await context.SubTasks.SingleAsync();
        Assert.Equal(created.Id, stored.TaskItemId);
    }

    [Fact]
    public async Task ToggleSubTaskAsync_FlipsSubTaskIsDone()
    {
        var created = await CreateTaskAsync("Parent", Priority.Medium);
        var subTask = await AddSubTaskAsync(created.Id, "Child");

        var toggled = await ToggleSubTaskAsync(created.Id, subTask!.Id);

        Assert.NotNull(toggled);
        Assert.True(toggled.IsDone);

        await using var context = _database.CreateContext();
        Assert.True((await context.SubTasks.SingleAsync()).IsDone);
    }

    [Fact]
    public async Task ToggleSubTaskAsync_WhenSubTaskBelongsToAnotherTask_ReturnsNull()
    {
        var owner = await CreateTaskAsync("Owner", Priority.Medium);
        var other = await CreateTaskAsync("Other", Priority.Medium);
        var subTask = await AddSubTaskAsync(owner.Id, "Child");

        Assert.Null(await ToggleSubTaskAsync(other.Id, subTask!.Id));
        Assert.Null(await ToggleSubTaskAsync(MissingId, subTask.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskAndCascadesToSubTasks()
    {
        var created = await CreateTaskAsync("Doomed", Priority.Medium);
        await AddSubTaskAsync(created.Id, "Also doomed");

        await using (var context = _database.CreateContext())
        {
            Assert.True(await new TaskService(context).DeleteAsync(created.Id, _ct));
        }

        await using var verify = _database.CreateContext();
        Assert.False(await verify.Tasks.AnyAsync());
        Assert.False(await verify.SubTasks.AnyAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        Assert.Null(await new TaskService(context).GetByIdAsync(MissingId, _ct));
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        var result = await new TaskService(context).UpdateAsync(
            MissingId, new UpdateTaskRequest { Title = "Nothing" }, _ct);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        await using var context = _database.CreateContext();

        Assert.False(await new TaskService(context).DeleteAsync(MissingId, _ct));
    }

    [Fact]
    public async Task ToggleAsync_WhenMissing_ReturnsNull()
    {
        Assert.Null(await ToggleAsync(MissingId));
    }

    [Fact]
    public async Task AddSubTaskAsync_WhenTaskMissing_ReturnsNull()
    {
        Assert.Null(await AddSubTaskAsync(MissingId, "Orphan"));
    }

    private async Task<TaskResponse> CreateTaskAsync(string title, Priority priority, string? description = null)
    {
        await using var context = _database.CreateContext();
        return await new TaskService(context).CreateAsync(
            new CreateTaskRequest { Title = title, Priority = priority, Description = description }, _ct);
    }

    private async Task<TaskResponse?> ToggleAsync(int id)
    {
        await using var context = _database.CreateContext();
        return await new TaskService(context).ToggleAsync(id, _ct);
    }

    private async Task<SubTaskResponse?> AddSubTaskAsync(int taskId, string title)
    {
        await using var context = _database.CreateContext();
        return await new TaskService(context).AddSubTaskAsync(
            taskId, new CreateSubTaskRequest { Title = title }, _ct);
    }

    private async Task<SubTaskResponse?> ToggleSubTaskAsync(int taskId, int subTaskId)
    {
        await using var context = _database.CreateContext();
        return await new TaskService(context).ToggleSubTaskAsync(taskId, subTaskId, _ct);
    }

    private async Task<IReadOnlyList<TaskResponse>> GetAllAsync(TaskStatusFilter? status, Priority? priority)
    {
        await using var context = _database.CreateContext();
        return await new TaskService(context).GetAllAsync(status, priority, _ct);
    }
}
