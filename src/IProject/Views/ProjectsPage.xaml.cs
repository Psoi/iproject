using IProject.ViewModels;

namespace IProject.Views;

public partial class ProjectsPage : ContentPage
{
    readonly ProjectsViewModel viewModel;

    public ProjectsPage(ProjectsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    // Also runs when coming back from a project, so progress is current.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
