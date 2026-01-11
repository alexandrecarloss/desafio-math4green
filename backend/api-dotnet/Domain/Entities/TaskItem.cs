using api_dotnet.Domain.Exceptions;

namespace api_dotnet.Domain.Entities
{
    public class TaskItem
    {
        protected TaskItem() { }

        public TaskItem(string title)
        {
            Title = title;
            Status = WorkStatus.Pending;
        }

        public int Id { get; private set; }
        public string Title { get; private set; } = null!;
        public WorkStatus Status { get; private set; }
        public bool IsBlocked => Dependencies.Any(d => d.PrerequisiteTask.Status != WorkStatus.Done);
        public int? AssignedUserId { get; private set; }
        public User? AssignedUser { get; private set; }

        public ICollection<TaskDependency> Dependencies { get; private set; } = new List<TaskDependency>();
        public ICollection<TaskDependency> IsPrerequisiteFor { get; private set; } = new List<TaskDependency>();

        public void AssignTo(User? user)
        {
            if (user == null)
            {
                if (Status == WorkStatus.InProgress || Status == WorkStatus.Done)
                {
                    throw new DomainException($"Não é permitido remover o responsável de uma tarefa com status {Status}.");
                }

                AssignedUser = null;
                AssignedUserId = null;
                return;
            }
            if (Status == WorkStatus.InProgress && user.HasTaskInProgress())
            {
                throw new DomainException($"O usuário {user.Name} já possui uma tarefa em andamento.");
            }

            AssignedUser = user;
            AssignedUserId = user.Id;
        }

        public void Start()
        {
            if (AssignedUser == null)
                throw new DomainException("Tarefa não atribuída.");

            if (AssignedUser.HasTaskInProgress())
                throw new DomainException("Usuário já possui uma tarefa em andamento.");

            if (Status != WorkStatus.Pending)
                throw new DomainException("Somente tarefas pendentes podem ser iniciadas.");

            if (Dependencies.Any(d => !d.PrerequisiteTask.IsCompleted()))
                throw new DomainException("Existem dependências pendentes.");

            Status = WorkStatus.InProgress;
        }

        public void Complete()
        {
            if (Dependencies.Any(d => !d.PrerequisiteTask.IsCompleted()))
                throw new DomainException("Não é possível concluir: existem dependências pendentes.");

            if (Status != WorkStatus.InProgress)
                throw new DomainException("Somente tarefas em andamento podem ser concluídas.");

            Status = WorkStatus.Done;
        }

        public bool IsCompleted() => Status == WorkStatus.Done;

        public void AddDependency(TaskItem prerequisite)
        {
            if (prerequisite.Id == this.Id)
                throw new DomainException("Uma tarefa não pode depender de si mesma.");

            if (Dependencies.Any(d => d.PrerequisiteTaskId == prerequisite.Id))
                return;

            Dependencies.Add(new TaskDependency(this, prerequisite));
        }

        public void ChangeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Título inválido");

            Title = title;
        }

        public void UpdateStatus(WorkStatus newStatus)
        {
            switch (newStatus)
            {
                case WorkStatus.Pending:
                    if (Status != WorkStatus.Pending)
                        throw new DomainException("Não é possível voltar para Pendente.");
                    break;

                case WorkStatus.InProgress:
                    Start();
                    break;

                case WorkStatus.Done:
                    Complete();
                    break;

                default:
                    throw new DomainException("Status inválido");
            }
        }

    }

}
