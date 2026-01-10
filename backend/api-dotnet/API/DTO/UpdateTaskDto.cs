using api_dotnet.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api_dotnet.API.DTO
{
    public class UpdateTaskDto
    {
        public string? Title { get; set; } = null!;
        public bool? Complete { get; set; }
        public int? AssignedUserId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(WorkStatus))]
        [SwaggerSchema("Status da tarefa")]
        public WorkStatus? Status { get; set; }
    }
}
