using IProject.Models;
using IProject.Views;

namespace IProject.ViewModels;

/// <summary>A task as shown in a project's list.</summary>
public class TaskRow(TaskItem task, (int Done, int Total) checklist)
{
    public TaskItem Task { get; } = task;

    public string Title => Task.Title;

    public bool IsDone => Task.Status == TaskState.Done;

    public Color PriorityColor => Palette.For(Task.Priority);

    public string TypeText => Task.Type.ToString().ToUpperInvariant();

    public Color TypeColor => Palette.For(Task.Type);

    // Status circle: empty ring (to do), ring with a dot (in progress), filled with a check (done).
    public Color StatusColor => Palette.For(Task.Status);

    public bool IsInProgress => Task.Status == TaskState.InProgress;

    public bool HasDueDate => Task.DueDate.HasValue;

    public string DueText => Task.DueDate is { } due
        ? due.Date == DateTime.Today ? "Today"
        : due.Date == DateTime.Today.AddDays(1) ? "Tomorrow"
        : due.ToString(due.Year == DateTime.Today.Year ? "d MMM" : "d MMM yyyy")
        : "";

    public Color DueColor => Task.IsOverdue ? Palette.Danger : Palette.TextSecondary;

    public bool HasChecklist => checklist.Total > 0;

    public string ChecklistText => $"☑ {checklist.Done}/{checklist.Total}";
}
