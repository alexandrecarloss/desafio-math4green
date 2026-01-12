using api_dotnet.Data;
using api_dotnet.Domain.Entities;
using api_dotnet.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace api_dotnet.Services;

public class TaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    // GET ALL
    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _context.TasksWithDetails.ToListAsync();
    }

    // GET BY ID
    public async Task<TaskItem> GetByIdAsync(int id)
    {
        var task = await _context.TasksWithDetails
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            throw new DomainException("Tarefa não encontrada");

        return task;
    }

    // CREATE
    public async Task<TaskItem> CreateAsync(string title, int? userId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título inválido");

        var task = new TaskItem(title);

        if (userId.HasValue)
        {
            var user = await LoadUser(userId.Value);
            task.AssignTo(user);
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    // UPDATE
    public async Task<TaskItem> UpdateAsync(int id, TaskCreateUpdateDto dto)
    {
        var task = await GetByIdAsync(id);

        if (dto.Title != null)
            task.ChangeTitle(dto.Title);

        if (dto.AssignedUserId.HasValue)
        {
            if (dto.AssignedUserId != task.AssignedUserId)
            {
                var user = await LoadUser(dto.AssignedUserId.Value);

                if (dto.Status == WorkStatus.InProgress || (dto.Status == null && task.Status == WorkStatus.InProgress))
                {
                    await ValidateUserCanStartTask(user.Id, task.Id);
                }

                task.AssignTo(user);
            }
        }
        else if (dto.AssignedUserId == null)
        {
            var finalStatus = dto.Status ?? task.Status;

            if (finalStatus == WorkStatus.Pending)
            {
                task.AssignTo(null);
            }
            else if (task.AssignedUserId != null)
            {
                task.AssignTo(null);
            }
        }

        if (dto.Status.HasValue && dto.Status.Value != task.Status)
        {
            if (dto.Status == WorkStatus.InProgress)
            {
                if (task.AssignedUserId == null)
                    throw new DomainException("Tarefa precisa de responsável para iniciar.");

                await ValidateUserCanStartTask(task.AssignedUserId.Value, task.Id);
            }

            task.UpdateStatus(dto.Status.Value);
        }

        await _context.SaveChangesAsync();
        return task;
    }

    // DELETE
    public async Task DeleteAsync(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Dependencies)
            .Include(t => t.IsPrerequisiteFor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            throw new DomainException("Tarefa não encontrada");

        _context.Set<TaskDependency>().RemoveRange(task.Dependencies);
        _context.Set<TaskDependency>().RemoveRange(task.IsPrerequisiteFor);
        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();
    }

    // DEPENDENCIES
    public async Task SyncDependenciesAsync(int id, List<int> prerequisiteIds)
    {
        var task = await _context.Tasks
            .Include(t => t.Dependencies)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            throw new DomainException("Tarefa não encontrada");

        foreach (var preId in prerequisiteIds)
            if (await WouldCreateCycle(id, preId))
                throw new DomainException("Ciclo de dependência detectado");

        task.Dependencies.Clear();

        foreach (var preId in prerequisiteIds)
        {
            var pre = await _context.Tasks.FindAsync(preId);
            if (pre != null)
                task.AddDependency(pre);
        }

        await _context.SaveChangesAsync();
    }

    // ===== Helpers =====

    private async Task<User> LoadUser(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Tasks)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new DomainException("Usuário não encontrado");

        return user;
    }

    private async Task<bool> WouldCreateCycle(int currentTaskId, int potentialPrerequisiteId)
    {
        if (currentTaskId == potentialPrerequisiteId)
            return true;

        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(potentialPrerequisiteId);

        while (stack.Count > 0)
        {
            var nextId = stack.Pop();
            if (nextId == currentTaskId) return true;

            if (visited.Add(nextId))
            {
                var deps = await _context.Set<TaskDependency>()
                    .Where(d => d.TaskId == nextId)
                    .Select(d => d.PrerequisiteTaskId)
                    .ToListAsync();

                foreach (var depId in deps)
                    stack.Push(depId);
            }
        }

        return false;
    }

    private async Task ValidateUserCanStartTask(int userId, int currentTaskId)
    {
        var hasAnotherInProgress = await _context.Tasks
            .AnyAsync(t =>
                t.AssignedUserId == userId &&
                t.Status == WorkStatus.InProgress &&
                t.Id != currentTaskId
            );

        if (hasAnotherInProgress)
            throw new DomainException("Usuário já possui uma tarefa em andamento.");
    }

}
