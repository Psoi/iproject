using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IProject.Models;
using IProject.Services;
using IProject.Views;

namespace IProject.ViewModels;

/// <summary>One project: To do / In progress / Done tabs and the quick-add bar.</summary>
public partial class ProjectViewModel(Database database) : ObservableObject, IQueryAttributable
{
    int projectId;
    List<TaskItem> allTasks = [];
    Dictionary<int, (int Done, int Total)> checklists = [];

    public ObservableCollection<TaskRow> Tasks { get; } = [];

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsToDoTab), nameof(IsInProgressTab), nameof(IsDoneTab), nameof(EmptyText))]
    private TaskState selectedTab;

    public bool IsToDoTab => SelectedTab == TaskState.ToDo;
    public bool IsInProgressTab => SelectedTab == TaskState.InProgress;
    public bool IsDoneTab => SelectedTab == TaskState.Done;

    [ObservableProperty]
    private string toDoTabText = "To do";

    [ObservableProperty]
    private string inProgressTabText = "In progress";

    [ObservableProperty]
    private string doneTabText = "Done";

    public string EmptyText => SelectedTab switch
    {
        TaskState.ToDo => "Nothing to do. Add a task below.",
        TaskState.InProgress => "Nothing in progress. Tap a task's circle to start it.",
        _ => "Nothing done yet.",
    };

    // ---- Quick add ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTypeText), nameof(NewTypeColor))]
    private TaskType newType;

    public string NewTypeText => NewType.ToString();

    public Color NewTypeColor => Palette.For(NewType);

    [ObservableProperty]
    private string newTitle = "";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            projectId = int.Parse(id.ToString()!);
    }

    public async Task LoadAsync()
    {
        var project = await database.GetProjectAsync(projectId);
        if (project == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }
        Title = project.Name;
        allTasks = await database.GetTasksAsync(projectId);
        checklists = await database.GetChecklistProgressAsync(projectId);
        Refresh();
    }

    void Refresh()
    {
        ToDoTabText = TabText(TaskState.ToDo);
        InProgressTabText = TabText(TaskState.InProgress);
        DoneTabText = TabText(TaskState.Done);

        // allTasks is already sorted by priority, then due date.
        Tasks.Clear();
        foreach (var task in allTasks.Where(t => t.Status == SelectedTab))
            Tasks.Add(new TaskRow(task, checklists.GetValueOrDefault(task.Id)));
    }

    string TabText(TaskState state) => $"{state.Label()}  {allTasks.Count(t => t.Status == state)}";

    [RelayCommand]
    void SelectTab(TaskState state)
    {
        SelectedTab = state;
        Refresh();
    }

    [RelayCommand]
    async Task AdvanceAsync(TaskRow row)
    {
        row.Task.Status = row.Task.Status.Next();
        await database.SaveTaskAsync(row.Task);
        Refresh();
    }

    [RelayCommand]
    async Task OpenTaskAsync(TaskRow row) =>
        await Shell.Current.GoToAsync($"{nameof(TaskDetailPage)}?id={row.Task.Id}");

    [RelayCommand]
    void ToggleNewType() => NewType = NewType.Next();

    [RelayCommand]
    async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
            return;

        // Lands in the tab you're looking at, so it doesn't vanish from view.
        var task = new TaskItem { ProjectId = projectId, Title = NewTitle.Trim(), Type = NewType, Status = SelectedTab };
        await database.SaveTaskAsync(task);
        NewTitle = "";
        allTasks = await database.GetTasksAsync(projectId);
        Refresh();
    }
}
