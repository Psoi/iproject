using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IProject.Models;
using IProject.Services;
using IProject.Views;

namespace IProject.ViewModels;

/// <summary>Main screen: all projects with progress, finished ones at the bottom.</summary>
public partial class ProjectsViewModel(Database database, BackupService backup) : ObservableObject
{
    public ObservableCollection<ProjectSummary> Projects { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        var summaries = await database.GetProjectSummariesAsync();
        Projects.Clear();
        foreach (var s in summaries)
            Projects.Add(s);
    }

    [RelayCommand]
    async Task NewProjectAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("New project", null, "Create", "Cancel", "Project name");
        if (string.IsNullOrWhiteSpace(name))
            return;
        await database.SaveProjectAsync(new Project { Name = name.Trim() });
        await LoadAsync();
    }

    [RelayCommand]
    async Task OpenAsync(ProjectSummary summary) =>
        await Shell.Current.GoToAsync($"{nameof(ProjectPage)}?id={summary.Project.Id}");

    [RelayCommand]
    async Task ShowMenuAsync(ProjectSummary summary)
    {
        var choice = await Shell.Current.DisplayActionSheet(summary.Name, "Cancel", "Delete", "Rename");
        if (choice == "Rename")
        {
            var name = await Shell.Current.DisplayPromptAsync("Rename project", null, "Save", "Cancel",
                initialValue: summary.Name);
            if (string.IsNullOrWhiteSpace(name))
                return;
            summary.Project.Name = name.Trim();
            await database.SaveProjectAsync(summary.Project);
            await LoadAsync();
        }
        else if (choice == "Delete")
        {
            var tasks = summary.Total == 1 ? "1 task" : $"{summary.Total} tasks";
            if (!await Shell.Current.DisplayAlert("Delete project?",
                    $"\"{summary.Name}\" and its {tasks}, with their attachments, will be deleted.", "Delete", "Cancel"))
                return;
            await database.DeleteProjectAsync(summary.Project);
            await LoadAsync();
        }
    }

    [RelayCommand]
    async Task BackupAsync()
    {
        var path = await backup.CreateAsync();
        await Share.Default.RequestAsync(new ShareFileRequest("Save iProject backup", new ShareFile(path, "application/zip")));
    }

    [RelayCommand]
    async Task RestoreAsync()
    {
        if (!await Shell.Current.DisplayAlert("Restore backup?",
                "Everything currently in the app is replaced by the backup.", "Choose file", "Cancel"))
            return;

        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose an iProject backup" });
        if (file == null)
            return;

        try
        {
            await backup.RestoreAsync(file);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            await Shell.Current.DisplayAlert("Couldn't restore", ex.Message, "OK");
        }
        await LoadAsync();
    }
}
