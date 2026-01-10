namespace api_dotnet.Domain.Entities
{
    public class TaskDependency
    {
        protected TaskDependency() { }

        public TaskDependency(TaskItem task, TaskItem prerequisite)
        {
            Task = task;
            TaskId = task.Id;

            PrerequisiteTask = prerequisite;
            PrerequisiteTaskId = prerequisite.Id;
        }

        public int Id { get; private set; }

        public int TaskId { get; private set; }
        public TaskItem Task { get; private set; } = null!;

        public int PrerequisiteTaskId { get; private set; }
        public TaskItem PrerequisiteTask { get; private set; } = null!;
    }

}
