using api_dotnet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_dotnet.Infrastructure.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
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

            builder.HasOne(t => t.AssignedUser)
                   .WithMany(u => u.Tasks)
                   .HasForeignKey(t => t.AssignedUserId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(t => t.Dependencies)
                   .WithOne(d => d.Task)
                   .HasForeignKey(d => d.TaskId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.IsPrerequisiteFor)
                   .WithOne(d => d.PrerequisiteTask)
                   .HasForeignKey(d => d.PrerequisiteTaskId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
