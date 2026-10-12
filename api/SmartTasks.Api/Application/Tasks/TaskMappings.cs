using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Application.Tasks;

internal static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Priority,
        task.IsDone,
        task.DueDate,
        task.CreatedAt,
        task.ModifiedAt,
        task.SubTasks.Select(s => s.ToResponse()).ToList());

    public static SubTaskResponse ToResponse(this SubTask subTask) =>
        new(subTask.Id, subTask.Title, subTask.IsDone);
}
