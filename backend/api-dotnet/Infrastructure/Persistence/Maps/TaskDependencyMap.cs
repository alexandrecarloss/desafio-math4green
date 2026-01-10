using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api_dotnet.Domain.Entities;

namespace api_dotnet.Infrastructure.Persistence.Configurations
{
    public class TaskDependencyMap : IEntityTypeConfiguration<TaskDependency>
    {
        public void Configure(EntityTypeBuilder<TaskDependency> builder)
        {
            builder.ToTable("TaskDependencies");

            builder.HasKey(d => d.Id);

            builder.HasOne(d => d.Task)
                   .WithMany(t => t.Dependencies)
                   .HasForeignKey(d => d.TaskId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.PrerequisiteTask)
                   .WithMany(t => t.IsPrerequisiteFor)
                   .HasForeignKey(d => d.PrerequisiteTaskId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
