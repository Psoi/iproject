using IProject.ViewModels;

namespace IProject.Views;

public partial class TaskDetailPage : ContentPage
{
    readonly TaskDetailViewModel viewModel;
    bool loaded;

    public TaskDetailPage(TaskDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load once: returning from a file picker or viewer must not throw away unsaved typing.
        if (loaded)
            return;
        loaded = true;
        await viewModel.LoadAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await viewModel.SaveAsync();
    }
}
