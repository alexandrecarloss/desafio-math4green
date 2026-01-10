using api_dotnet.Domain.Entities;

namespace api_dotnet.API.DTO
{
    public class UpdateTaskDto
    {
        public string? Title { get; set; } = null!;
        public bool? Complete { get; set; }
        public int? AssignedUserId { get; set; }
        public WorkStatus? Status { get; set; }
    }
}
