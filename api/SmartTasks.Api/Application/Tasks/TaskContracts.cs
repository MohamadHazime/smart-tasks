using System.ComponentModel.DataAnnotations;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Application.Tasks;

public enum TaskStatusFilter
{
    Open,
    Done
}

public sealed record CreateTaskRequest
{
    [Required, MaxLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? DueDate { get; init; }
}

public sealed record UpdateTaskRequest
{
    [Required, MaxLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? DueDate { get; init; }
}

public sealed record CreateSubTaskRequest
{
    [Required, MaxLength(SubTask.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;
}

public sealed record TaskResponse(
    int Id,
    string Title,
    string? Description,
    Priority Priority,
    bool IsDone,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IReadOnlyList<SubTaskResponse> SubTasks);

public sealed record SubTaskResponse(int Id, string Title, bool IsDone);

public sealed record SubTaskSuggestionsResponse(int TaskId, IReadOnlyList<string> Suggestions);
