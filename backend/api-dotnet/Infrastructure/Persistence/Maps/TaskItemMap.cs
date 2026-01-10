using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api_dotnet.Domain.Entities;

namespace api_dotnet.Infrastructure.Persistence.Configurations
{
    public class TaskItemMap : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(t => t.Status)
                   .IsRequired();

            builder.HasMany(t => t.Dependencies)
                   .WithOne(d => d.Task)
                   .HasForeignKey(d => d.TaskId);

            builder.HasMany(t => t.IsPrerequisiteFor)
                   .WithOne(d => d.PrerequisiteTask)
                   .HasForeignKey(d => d.PrerequisiteTaskId);
        }
    }
}
