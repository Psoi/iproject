using Android.Util;
using AndroidX.Core.Content;
using IProject.Services;
using IProject.ViewModels;
using IProject.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace IProject;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<Database>();
		builder.Services.AddSingleton<BackupService>();
		builder.Services.AddTransient<ProjectsViewModel>();
		builder.Services.AddTransient<ProjectsPage>();
		builder.Services.AddTransient<ProjectViewModel>();
		builder.Services.AddTransient<ProjectPage>();
		builder.Services.AddTransient<TaskDetailViewModel>();
		builder.Services.AddTransient<TaskDetailPage>();

		ConfigurePressableGrid();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	/// <summary>Native tap, long press and ripple for <see cref="PressableGrid"/>.</summary>
	static void ConfigurePressableGrid()
	{
		LayoutHandler.Mapper.AppendToMapping("PressableGridGestures", (handler, view) =>
		{
			if (view is not PressableGrid row)
				return;

			var native = handler.PlatformView;
			native.Clickable = true;
			native.LongClickable = true;
			native.Click += (_, _) => Execute(row.Command, row.CommandParameter);
			native.LongClick += (_, e) =>
			{
				Execute(row.LongPressCommand, row.CommandParameter);
				e.Handled = true;
			};

			var ripple = new TypedValue();
			if (native.Context!.Theme!.ResolveAttribute(Android.Resource.Attribute.SelectableItemBackground, ripple, true))
				native.Foreground = ContextCompat.GetDrawable(native.Context, ripple.ResourceId);
		});
	}

	static void Execute(System.Windows.Input.ICommand? command, object? parameter)
	{
		if (command?.CanExecute(parameter) == true)
			command.Execute(parameter);
	}
}
