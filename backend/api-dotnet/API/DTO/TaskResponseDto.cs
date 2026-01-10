using api_dotnet.Domain.Entities;

namespace api_dotnet.API.DTO
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public WorkStatus Status { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public bool IsBlocked { get; set; }
        public List<string> PrerequisiteTitles { get; set; } = new();

        public static TaskResponseDto FromEntity(TaskItem task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                AssignedUserId = task.AssignedUserId,
                AssignedUserName = task.AssignedUser?.Name ?? "Não atribuída",
                IsBlocked = task.Dependencies.Any(d => d.PrerequisiteTask.Status != WorkStatus.Done),
                PrerequisiteTitles = task.Dependencies
                    .Select(d => d.PrerequisiteTask.Title)
                    .ToList()
            };
        }
    }
}