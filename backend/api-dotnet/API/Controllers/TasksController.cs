using api_dotnet.API.DTO;
using api_dotnet.Domain.Entities;
using api_dotnet.Domain.Exceptions;
using api_dotnet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api_dotnet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/tasks
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _context.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.Dependencies)
                .ThenInclude(d => d.PrerequisiteTask)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                AssignedUserId = t.AssignedUserId,
                AssignedUserName = t.AssignedUser != null ? t.AssignedUser.Name : "Não atribuída",
                IsBlocked = t.Dependencies.Any(d => d.PrerequisiteTask.Status != WorkStatus.Done),
                PrerequisiteTitles = t.Dependencies.Select(d => d.PrerequisiteTask.Title).ToList()
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // GET api/tasks/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null) return NotFound();

        return Ok(TaskResponseDto.FromEntity(task));
    }

    // POST api/tasks
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("O título é obrigatório.");

        var task = new TaskItem(dto.Title);
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(TaskResponseDto.FromEntity(task));
    }

    // PUT api/tasks/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.Dependencies)
                .ThenInclude(d => d.PrerequisiteTask)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        try
        {
            if (!string.IsNullOrWhiteSpace(dto.Title))
                task.ChangeTitle(dto.Title);

            if (dto.AssignedUserId.HasValue && task.AssignedUserId != dto.AssignedUserId)
            {
                var user = await _context.Users
                    .Include(u => u.Tasks)
                    .FirstOrDefaultAsync(u => u.Id == dto.AssignedUserId.Value);

                if (user == null) return BadRequest("Usuário não encontrado.");

                if (task.Status == WorkStatus.InProgress)
                {
                    var hasTaskInProgress = await _context.Tasks.AnyAsync(t =>
                        t.AssignedUserId == dto.AssignedUserId &&
                        t.Status == WorkStatus.InProgress &&
                        t.Id != id);

                    if (hasTaskInProgress)
                        return BadRequest(new { message = $"O usuário {user.Name} já possui uma tarefa em andamento." });
                }

                task.AssignTo(user);
            }

            if (dto.Status.HasValue && task.Status != dto.Status.Value)
            {
                if (dto.Status == WorkStatus.InProgress)
                {
                    task.Start();
                }
                else if (dto.Status == WorkStatus.Done)
                {
                    task.Complete();
                }
                else
                {
                    task.UpdateStatus(dto.Status.Value);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                IsBlocked = task.Dependencies.Any(d => d.PrerequisiteTask.Status != WorkStatus.Done),
                AssignedUserName = task.AssignedUser?.Name
            });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    // DELETE api/tasks/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Dependencies)    
            .Include(t => t.IsPrerequisiteFor) 
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        _context.Set<TaskDependency>().RemoveRange(task.Dependencies);
        _context.Set<TaskDependency>().RemoveRange(task.IsPrerequisiteFor);

        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST api/tasks/{id}/dependencies/{prerequisiteId}
    [HttpPost("{id}/dependencies/sync")]
    public async Task<IActionResult> SyncDependencies(int id, [FromBody] List<int> prerequisiteIds)
    {
        var task = await _context.Tasks
            .Include(t => t.Dependencies)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        foreach (var preId in prerequisiteIds)
        {
            if (await WouldCreateCycle(id, preId))
            {
                var preTask = await _context.Tasks.FindAsync(preId);
                return BadRequest(new
                {
                    message = $"Conflito de Ciclo: A tarefa '{preTask?.Title}' não pode ser um pré-requisito porque ela já depende (direta ou indiretamente) desta tarefa atual!"
                });
            }
        }

        task.Dependencies.Clear();
        foreach (var preId in prerequisiteIds)
        {
            var pre = await _context.Tasks.FindAsync(preId);
            if (pre != null) task.AddDependency(pre);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task<bool> WouldCreateCycle(int currentTaskId, int potentialPrerequisiteId)
    {
        if (currentTaskId == potentialPrerequisiteId) return true;

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

                foreach (var depId in deps) stack.Push(depId);
            }
        }
        return false;
    }
}
