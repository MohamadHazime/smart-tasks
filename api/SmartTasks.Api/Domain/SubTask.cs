namespace SmartTasks.Api.Domain;

public class SubTask : Entity
{
    public const int TitleMaxLength = 200;

    private SubTask() { }

    internal SubTask(string title)
    {
        Title = Guard.RequiredText(title, TitleMaxLength, nameof(Title));
    }

    public int TaskItemId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsDone { get; private set; }

    public void Toggle() => IsDone = !IsDone;
}
