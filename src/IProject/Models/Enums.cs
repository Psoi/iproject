namespace IProject.Models;

public enum TaskType { Task, Bug, Feature }

// Not "TaskStatus": that name is taken by System.Threading.Tasks.
public enum TaskState { ToDo, InProgress, Done }

public enum Priority { Low, Normal, High, Urgent }

public static class EnumText
{
    public static string Label(this TaskState state) => state switch
    {
        TaskState.ToDo => "To do",
        TaskState.InProgress => "In progress",
        _ => "Done",
    };

    /// <summary>To do → In progress → Done → back to To do.</summary>
    public static TaskState Next(this TaskState state) => (TaskState)(((int)state + 1) % 3);

    public static TaskType Next(this TaskType type) => (TaskType)(((int)type + 1) % 3);
}
