using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IProject.Models;
using IProject.Services;

namespace IProject.ViewModels;

/// <summary>
/// Task detail. Every change is saved as you make it; typing in the title and description
/// is saved after a short pause and again when you leave the page.
/// </summary>
public partial class TaskDetailViewModel(Database database) : ObservableObject, IQueryAttributable
{
    int taskId;
    TaskItem? task;
    bool loading;
    CancellationTokenSource? pendingSave;

    public ObservableCollection<ChecklistRow> Checklist { get; } = [];
    public ObservableCollection<Attachment> Attachments { get; } = [];
    public ObservableCollection<Comment> Comments { get; } = [];

    public IList<string> TypeOptions { get; } = ["Task", "Bug", "Feature"];
    public IList<string> StatusOptions { get; } = ["To do", "In progress", "Done"];
    public IList<string> PriorityOptions { get; } = ["Low", "Normal", "High", "Urgent"];
    public IList<Color> TypeColors { get; } = Enum.GetValues<TaskType>().Select(Views.Palette.For).ToList();
    public IList<Color> StatusColors { get; } = Enum.GetValues<TaskState>().Select(Views.Palette.For).ToList();
    public IList<Color> PriorityColors { get; } = Enum.GetValues<Priority>().Select(Views.Palette.For).ToList();

    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private int typeIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverdue))]
    private int statusIndex;

    [ObservableProperty]
    private int priorityIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverdue))]
    private bool hasDueDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverdue))]
    private DateTime dueDate = DateTime.Today;

    public bool IsOverdue => HasDueDate && DueDate.Date < DateTime.Today && StatusIndex != (int)TaskState.Done;

    [ObservableProperty]
    private string checklistSummary = "";

    [ObservableProperty]
    private string newChecklistText = "";

    [ObservableProperty]
    private string newComment = "";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            taskId = int.Parse(id.ToString()!);
    }

    public async Task LoadAsync()
    {
        task = await database.GetTaskAsync(taskId);
        if (task == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        loading = true;
        Title = task.Title;
        Description = task.Description;
        TypeIndex = (int)task.Type;
        StatusIndex = (int)task.Status;
        PriorityIndex = (int)task.Priority;
        HasDueDate = task.DueDate.HasValue;
        DueDate = task.DueDate ?? DateTime.Today;
        loading = false;

        Checklist.Clear();
        foreach (var item in await database.GetChecklistAsync(taskId))
            Checklist.Add(new ChecklistRow(item, OnChecklistToggled));
        UpdateChecklistSummary();

        Attachments.Clear();
        foreach (var a in await database.GetAttachmentsAsync(taskId))
            Attachments.Add(a);

        Comments.Clear();
        foreach (var c in await database.GetCommentsAsync(taskId))
            Comments.Add(c);
    }

    // ---- Fields ----

    partial void OnTitleChanged(string value) => SaveSoon();
    partial void OnDescriptionChanged(string value) => SaveSoon();
    partial void OnTypeIndexChanged(int value) => _ = SaveAsync();
    partial void OnStatusIndexChanged(int value) => _ = SaveAsync();
    partial void OnPriorityIndexChanged(int value) => _ = SaveAsync();
    partial void OnHasDueDateChanged(bool value) => _ = SaveAsync();
    partial void OnDueDateChanged(DateTime value) => _ = SaveAsync();

    [RelayCommand]
    void SetDueDate()
    {
        DueDate = DateTime.Today;
        HasDueDate = true;
    }

    [RelayCommand]
    void ClearDueDate() => HasDueDate = false;

    async void SaveSoon()
    {
        if (loading)
            return;
        pendingSave?.Cancel();
        var cts = pendingSave = new CancellationTokenSource();
        try
        {
            await Task.Delay(600, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return; // more typing, or saved some other way
        }
        await SaveAsync();
    }

    /// <summary>Writes the fields back. Also called when the page goes away.</summary>
    public async Task SaveAsync()
    {
        if (loading || task == null)
            return;
        pendingSave?.Cancel();

        // An emptied title would leave a blank row; keep the old one instead.
        if (!string.IsNullOrWhiteSpace(Title))
            task.Title = Title.Trim();
        task.Description = Description ?? "";
        task.Type = (TaskType)TypeIndex;
        task.Status = (TaskState)StatusIndex;
        task.Priority = (Priority)PriorityIndex;
        task.DueDate = HasDueDate ? DueDate.Date : null;
        await database.SaveTaskAsync(task);
    }

    // ---- Checklist ----

    [RelayCommand]
    async Task AddChecklistItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewChecklistText))
            return;
        var item = new ChecklistItem { TaskId = taskId, Text = NewChecklistText.Trim() };
        await database.SaveChecklistItemAsync(item);
        Checklist.Add(new ChecklistRow(item, OnChecklistToggled));
        NewChecklistText = "";
        UpdateChecklistSummary();
    }

    [RelayCommand]
    async Task DeleteChecklistItemAsync(ChecklistRow row)
    {
        await database.DeleteChecklistItemAsync(row.Item);
        Checklist.Remove(row);
        UpdateChecklistSummary();
    }

    async void OnChecklistToggled(ChecklistRow row)
    {
        await database.SaveChecklistItemAsync(row.Item);
        UpdateChecklistSummary();
    }

    void UpdateChecklistSummary() => ChecklistSummary = Checklist.Count == 0
        ? "CHECKLIST"
        : $"CHECKLIST  {Checklist.Count(c => c.IsDone)}/{Checklist.Count}";

    // ---- Attachments ----

    [RelayCommand]
    async Task AddPhotoAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync();
        await AddFilesAsync([photo]);
    }

    [RelayCommand]
    async Task AddFileAsync()
    {
        var files = await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Attach files" });
        await AddFilesAsync(files);
    }

    async Task AddFilesAsync(IEnumerable<FileResult?>? files)
    {
        foreach (var file in files ?? [])
        {
            if (file == null)
                continue;
            try
            {
                Attachments.Add(await database.AddAttachmentAsync(taskId, file));
            }
            catch (IOException ex)
            {
                await Shell.Current.DisplayAlert("Couldn't attach " + file.FileName, ex.Message, "OK");
            }
        }
    }

    [RelayCommand]
    async Task OpenAttachmentAsync(Attachment attachment)
    {
        if (!File.Exists(attachment.FullPath))
        {
            await Shell.Current.DisplayAlert("File missing", $"{attachment.FileName} is no longer on the device.", "OK");
            return;
        }
        await Launcher.Default.OpenAsync(new OpenFileRequest(attachment.FileName, new ReadOnlyFile(attachment.FullPath)));
    }

    [RelayCommand]
    async Task ShowAttachmentMenuAsync(Attachment attachment)
    {
        var choice = await Shell.Current.DisplayActionSheet(attachment.FileName, "Cancel", "Delete", "Open", "Share");
        if (choice == "Open")
            await OpenAttachmentAsync(attachment);
        else if (choice == "Share")
            await Share.Default.RequestAsync(new ShareFileRequest(attachment.FileName, new ShareFile(attachment.FullPath)));
        else if (choice == "Delete")
        {
            await database.DeleteAttachmentAsync(attachment);
            Attachments.Remove(attachment);
        }
    }

    // ---- Comments ----

    [RelayCommand]
    async Task PostCommentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewComment))
            return;
        var comment = new Comment { TaskId = taskId, Text = NewComment.Trim() };
        await database.AddCommentAsync(comment);
        Comments.Add(comment);
        NewComment = "";
    }

    [RelayCommand]
    async Task ShowCommentMenuAsync(Comment comment)
    {
        var choice = await Shell.Current.DisplayActionSheet(comment.Timestamp, "Cancel", "Delete", "Copy text");
        if (choice == "Copy text")
            await Clipboard.Default.SetTextAsync(comment.Text);
        else if (choice == "Delete")
        {
            await database.DeleteCommentAsync(comment);
            Comments.Remove(comment);
        }
    }

    // ---- Delete ----

    [RelayCommand]
    async Task DeleteTaskAsync()
    {
        if (task == null)
            return;
        if (!await Shell.Current.DisplayAlert("Delete task?",
                $"\"{task.Title}\" with its checklist, attachments and comments will be deleted.", "Delete", "Cancel"))
            return;

        pendingSave?.Cancel();
        await database.DeleteTaskAsync(task);
        task = null;
        await Shell.Current.GoToAsync("..");
    }
}

public partial class ChecklistRow : ObservableObject
{
    readonly Action<ChecklistRow> toggled;

    public ChecklistRow(ChecklistItem item, Action<ChecklistRow> toggled)
    {
        Item = item;
        isDone = item.IsDone;
        this.toggled = toggled;
    }

    public ChecklistItem Item { get; }

    public string Text => Item.Text;

    [ObservableProperty]
    private bool isDone;

    partial void OnIsDoneChanged(bool value)
    {
        Item.IsDone = value;
        toggled(this);
    }
}
