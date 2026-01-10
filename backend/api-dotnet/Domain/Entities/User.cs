using api_dotnet.Domain.Entities;
using api_dotnet.Domain.Exceptions;

public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;

    protected User() { }

    public User(string name)
    {
        ChangeName(name);
    }

    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome inválido");

        Name = name;
    }

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    public bool HasTaskInProgress()
    {
        return Tasks.Any(t => t.Status == WorkStatus.InProgress);
    }

}
