using IProject.Views;

namespace IProject;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ProjectPage), typeof(ProjectPage));
        Routing.RegisterRoute(nameof(TaskDetailPage), typeof(TaskDetailPage));
    }
}
