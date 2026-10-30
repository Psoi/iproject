using IProject.Models;

namespace IProject.Views;

/// <summary>Colors that depend on data (priority, type, status). Page colors are in App.xaml.</summary>
public static class Palette
{
    public static readonly Color Accent = Color.FromArgb("#3B82F6");
    public static readonly Color Danger = Color.FromArgb("#EF4444");
    public static readonly Color Success = Color.FromArgb("#22C55E");
    public static readonly Color Muted = Color.FromArgb("#6B7280");
    public static readonly Color TextSecondary = Color.FromArgb("#9E9E9E");

    public static Color For(Priority priority) => priority switch
    {
        Priority.Low => Color.FromArgb("#64748B"),
        Priority.Normal => Color.FromArgb("#3B82F6"),
        Priority.High => Color.FromArgb("#F59E0B"),
        _ => Color.FromArgb("#EF4444"),
    };

    public static Color For(TaskType type) => type switch
    {
        TaskType.Task => Color.FromArgb("#60A5FA"),
        TaskType.Bug => Color.FromArgb("#F87171"),
        _ => Color.FromArgb("#4ADE80"),
    };

    public static Color For(TaskState state) => state switch
    {
        TaskState.ToDo => Muted,
        TaskState.InProgress => Accent,
        _ => Success,
    };
}
