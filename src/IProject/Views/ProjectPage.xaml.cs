using IProject.ViewModels;

namespace IProject.Views;

public partial class ProjectPage : ContentPage
{
    readonly ProjectViewModel viewModel;

    public ProjectPage(ProjectViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    // Also runs when coming back from a task, which may have changed or been deleted.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
