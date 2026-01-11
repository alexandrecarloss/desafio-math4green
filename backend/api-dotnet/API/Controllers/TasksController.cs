using api_dotnet.Data;
using api_dotnet.Domain.Entities;
using api_dotnet.Domain.Exceptions;
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
        var tasks = await _context.TasksWithDetails.ToListAsync();

        var result = tasks.Select(TaskResponseDto.FromEntity).ToList();

        return Ok(result);
    }

    // GET api/tasks/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _context.TasksWithDetails
        .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        return Ok(TaskResponseDto.FromEntity(task));

    }

    // POST api/tasks
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "O título é obrigatório." });

        var task = new TaskItem(dto.Title);
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return Ok(TaskResponseDto.FromEntity(task));

    }

    // PUT api/tasks/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TaskCreateUpdateDto dto)
    {
        var task = await _context.TasksWithDetails
        .FirstOrDefaultAsync(t => t.Id == id);


        if (task == null) return NotFound();

        try
        {
            if (dto.Title != null)
                task.ChangeTitle(dto.Title);

            if (dto.AssignedUserId.HasValue)
            {
                var user = await _context.Users
                    .Include(u => u.Tasks)
                    .FirstOrDefaultAsync(u => u.Id == dto.AssignedUserId.Value);

                if (user == null)
                    return BadRequest(new { message = "Usuário não encontrado" });

                if (dto.Status == WorkStatus.InProgress && user.HasAnotherTaskInProgress(id))
                    return BadRequest(new { message = "Usuário já possui tarefa em andamento" });

                task.AssignTo(user);
            }

            if (dto.Status.HasValue)
                task.UpdateStatus(dto.Status.Value);


            await _context.SaveChangesAsync();

            return Ok(TaskResponseDto.FromEntity(task));
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
