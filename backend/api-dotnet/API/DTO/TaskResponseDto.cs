using api_dotnet.Domain.Entities;

public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public WorkStatus Status { get; set; }
    public bool IsBlocked { get; set; }

    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }

    public List<int> PrerequisiteIds { get; set; } = new();
    public List<string> PrerequisiteTitles { get; set; } = new();

    public static TaskResponseDto FromEntity(TaskItem task)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            IsBlocked = task.IsBlocked,

            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser?.Name,

            PrerequisiteIds = task.Dependencies
                .Select(d => d.PrerequisiteTaskId)
                .ToList(),

            PrerequisiteTitles = task.Dependencies
                .Select(d => d.PrerequisiteTask.Title)
                .ToList()
        };
    }
}
