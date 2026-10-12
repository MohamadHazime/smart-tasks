namespace SmartTasks.Api.Domain;

public class TaskItem : Entity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private readonly List<SubTask> _subTasks = new();

    private TaskItem() { }

    public TaskItem(string title, string? description, Priority priority, DateTime? dueDate)
    {
        Update(title, description, priority, dueDate);
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public bool IsDone { get; private set; }
    public DateTime? DueDate { get; private set; }

    public IReadOnlyCollection<SubTask> SubTasks => _subTasks.AsReadOnly();

    public void Update(string title, string? description, Priority priority, DateTime? dueDate)
    {
        Title = Guard.RequiredText(title, TitleMaxLength, nameof(Title));
        Description = Guard.OptionalText(description, DescriptionMaxLength, nameof(Description));
        Priority = Enum.IsDefined(priority)
            ? priority
            : throw new DomainException($"Unknown priority '{priority}'.");
        DueDate = dueDate;
    }

    public void Toggle() => IsDone = !IsDone;

    public SubTask AddSubTask(string title)
    {
        var subTask = new SubTask(title);
        _subTasks.Add(subTask);
        return subTask;
    }
}
