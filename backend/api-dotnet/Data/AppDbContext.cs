using Microsoft.EntityFrameworkCore;
using api_dotnet.Domain.Entities;

namespace api_dotnet.Data;

public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<User> Users => Set<User>();
    public IQueryable<TaskItem> TasksWithDetails =>
    Tasks
        .Include(t => t.AssignedUser)
        .Include(t => t.Dependencies)
            .ThenInclude(d => d.PrerequisiteTask);

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        model.Entity<User>().HasData(
            new User(1, "Antônio"),
            new User(2, "Bob"),
            new User(3, "Carlos"),
            new User(4, "Deysi")
        );

        model.Entity<TaskItem>()
            .HasOne(t => t.AssignedUser)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        model.Entity<TaskDependency>()
            .HasOne(d => d.Task)
            .WithMany(t => t.Dependencies)
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        model.Entity<TaskDependency>()
            .HasOne(d => d.PrerequisiteTask)
            .WithMany(t => t.IsPrerequisiteFor)
            .HasForeignKey(d => d.PrerequisiteTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<TaskDependency>()
            .HasIndex(d => new { d.TaskId, d.PrerequisiteTaskId })
            .IsUnique();
    }
}