using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Tests.Domain;

public class TaskItemTests
{
    [Fact]
    public void Constructor_TrimsTitleAndNormalizesBlankDescriptionToNull()
    {
        var task = new TaskItem("  Plan the day  ", "   ", Priority.High, dueDate: null);

        Assert.Equal("Plan the day", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(Priority.High, task.Priority);
        Assert.False(task.IsDone);
        Assert.Empty(task.SubTasks);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithBlankTitle_Throws(string? title)
    {
        var exception = Assert.Throws<DomainException>(() =>
            new TaskItem(title!, null, Priority.Medium, null));

        Assert.Equal("Title is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithTooLongTitle_Throws()
    {
        var title = new string('a', TaskItem.TitleMaxLength + 1);

        Assert.Throws<DomainException>(() => new TaskItem(title, null, Priority.Medium, null));
    }

    [Fact]
    public void Constructor_WithTooLongDescription_Throws()
    {
        var description = new string('a', TaskItem.DescriptionMaxLength + 1);

        Assert.Throws<DomainException>(() => new TaskItem("Title", description, Priority.Medium, null));
    }

    [Fact]
    public void Constructor_WithUndefinedPriority_Throws()
    {
        Assert.Throws<DomainException>(() => new TaskItem("Title", null, (Priority)99, null));
    }

    [Fact]
    public void Update_ReplacesEditableFields()
    {
        var task = new TaskItem("Old", "Old description", Priority.Low, null);
        var dueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        task.Update("New", "New description", Priority.High, dueDate);

        Assert.Equal("New", task.Title);
        Assert.Equal("New description", task.Description);
        Assert.Equal(Priority.High, task.Priority);
        Assert.Equal(dueDate, task.DueDate);
    }

    [Fact]
    public void Toggle_FlipsIsDone()
    {
        var task = new TaskItem("Title", null, Priority.Medium, null);

        task.Toggle();
        Assert.True(task.IsDone);

        task.Toggle();
        Assert.False(task.IsDone);
    }

    [Fact]
    public void AddSubTask_AddsTrimmedSubTaskToCollection()
    {
        var task = new TaskItem("Parent", null, Priority.Medium, null);

        var subTask = task.AddSubTask("  Child  ");

        Assert.Equal("Child", subTask.Title);
        Assert.Same(subTask, Assert.Single(task.SubTasks));
    }

    [Fact]
    public void AddSubTask_WithBlankTitle_ThrowsAndAddsNothing()
    {
        var task = new TaskItem("Parent", null, Priority.Medium, null);

        Assert.Throws<DomainException>(() => task.AddSubTask(" "));
        Assert.Empty(task.SubTasks);
    }
}
