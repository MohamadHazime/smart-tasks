using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTasks.Api.Domain;

namespace SmartTasks.Api.Infrastructure.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(TaskItem.TitleMaxLength);
        builder.Property(t => t.Description).HasMaxLength(TaskItem.DescriptionMaxLength);
        builder.HasIndex(t => t.IsDone);
        builder.HasIndex(t => t.Priority);

        builder.HasMany(t => t.SubTasks)
            .WithOne()
            .HasForeignKey(s => s.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.SubTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
