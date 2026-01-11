using api_dotnet.Domain.Entities;

public class TaskCreateUpdateDto
{
    public string? Title { get; set; }
    public WorkStatus? Status { get; set; }
    public int? AssignedUserId { get; set; }
    public List<int>? PrerequisiteIds { get; set; }
}
