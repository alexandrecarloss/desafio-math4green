using api_dotnet.Domain.Exceptions;
using api_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly TaskService _service;

    public TasksController(TaskService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _service.GetAllAsync();
        return Ok(tasks.Select(TaskResponseDto.FromEntity));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var task = await _service.GetByIdAsync(id);
            return Ok(TaskResponseDto.FromEntity(task));
        }
        catch (DomainException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskCreateUpdateDto dto)
    {
        try
        {
            var task = await _service.CreateAsync(dto.Title!, dto.AssignedUserId);
            return Ok(TaskResponseDto.FromEntity(task));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TaskCreateUpdateDto dto)
    {
        try
        {
            var task = await _service.UpdateAsync(id, dto);
            return Ok(TaskResponseDto.FromEntity(task));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/dependencies/sync")]
    public async Task<IActionResult> Sync(int id, List<int> ids)
    {
        try
        {
            await _service.SyncDependenciesAsync(id, ids);
            return Ok();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
