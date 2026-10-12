using Microsoft.EntityFrameworkCore;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Application.Tasks;

public sealed class TaskService(IAppDbContext db) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        TaskStatusFilter? status, Priority? priority, CancellationToken cancellationToken)
    {
        IQueryable<TaskItem> query = db.Tasks.AsNoTracking().Include(t => t.SubTasks);

        if (status == TaskStatusFilter.Open)
            query = query.Where(t => !t.IsDone);
        else if (status == TaskStatusFilter.Done)
            query = query.Where(t => t.IsDone);

        if (priority is not null)
            query = query.Where(t => t.Priority == priority.Value);

        var tasks = await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tasks.Select(t => t.ToResponse()).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return task?.ToResponse();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskItem(request.Title, request.Description, request.Priority, request.DueDate);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await FindWithSubTasksAsync(id, cancellationToken);
        if (task is null)
            return null;

        task.Update(request.Title, request.Description, request.Priority, request.DueDate);
        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
            return false;

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<TaskResponse?> ToggleAsync(int id, CancellationToken cancellationToken)
    {
        var task = await FindWithSubTasksAsync(id, cancellationToken);
        if (task is null)
            return null;

        task.Toggle();
        await db.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    public async Task<SubTaskResponse?> AddSubTaskAsync(
        int taskId, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
            return null;

        var subTask = task.AddSubTask(request.Title);
        await db.SaveChangesAsync(cancellationToken);

        return subTask.ToResponse();
    }

    public async Task<SubTaskResponse?> ToggleSubTaskAsync(
        int taskId, int subTaskId, CancellationToken cancellationToken)
    {
        var task = await FindWithSubTasksAsync(taskId, cancellationToken);
        var subTask = task?.ToggleSubTask(subTaskId);
        if (subTask is null)
            return null;

        await db.SaveChangesAsync(cancellationToken);

        return subTask.ToResponse();
    }

    private Task<TaskItem?> FindWithSubTasksAsync(int id, CancellationToken cancellationToken) =>
        db.Tasks
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
