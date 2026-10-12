using Microsoft.EntityFrameworkCore;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Application;

public interface IAppDbContext
{
    DbSet<TaskItem> Tasks { get; }
    DbSet<SubTask> SubTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
