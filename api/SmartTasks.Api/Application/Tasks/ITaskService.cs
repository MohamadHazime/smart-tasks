using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(TaskStatusFilter? status, Priority? priority, CancellationToken cancellationToken);
    Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<TaskResponse?> ToggleAsync(int id, CancellationToken cancellationToken);
    Task<SubTaskResponse?> AddSubTaskAsync(int taskId, CreateSubTaskRequest request, CancellationToken cancellationToken);
    Task<SubTaskResponse?> ToggleSubTaskAsync(int taskId, int subTaskId, CancellationToken cancellationToken);
}
