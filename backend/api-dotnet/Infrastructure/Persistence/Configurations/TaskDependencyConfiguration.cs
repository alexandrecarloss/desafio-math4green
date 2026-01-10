using api_dotnet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_dotnet.Infrastructure.Persistence.Configurations
{
    public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
    {
        public void Configure(EntityTypeBuilder<TaskDependency> builder)
        {
            builder.ToTable("TaskDependencies");

            builder.HasKey(d => d.Id);

            builder.HasOne(d => d.Task)
                   .WithMany(t => t.Dependencies)
                   .HasForeignKey(d => d.TaskId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.PrerequisiteTask)
                   .WithMany(t => t.IsPrerequisiteFor)
                   .HasForeignKey(d => d.PrerequisiteTaskId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => new { d.TaskId, d.PrerequisiteTaskId })
                   .IsUnique();
        }
    }
}
