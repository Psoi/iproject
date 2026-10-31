# iProject

A personal project management tool for Android. Track your own projects and tasks on your phone with nothing leaving the device: no account, no cloud, no sync.

Built with .NET MAUI and SQLite.

## Features

**Projects**
- Every project shows a progress bar and "X of Y done · %"
- Add, rename and delete projects
- Finished projects sink to the bottom

**Inside a project**
- To do / In progress / Done tabs, each with a count
- Tasks sorted by priority, then due date
- Tap a task's circle to move it to the next status
- Quick-add bar at the bottom with a Task / Bug / Feature toggle

**Task detail**
- Title, type (Task / Bug / Feature) and status (To do / In progress / Done)
- Priority (Low / Normal / High / Urgent), shown as a colored edge on the task's row
- Due date, which turns red when overdue
- Description
- Checklist of sub-steps you tick off
- Attachments (images, PDFs, any file), copied into the app's storage
- Comments as a timestamped log
- Delete

**Backup and restore**
- Saves everything (database and attachments) as one .zip, through the share sheet
- Restore replaces the app's data with a backup

## Requirements

- Android 8.0 or later
- To build: the .NET 9 SDK with the `android` workload (`dotnet workload install android`)

## Build and install

Debug build, installed and started on a connected phone or emulator:

```bash
dotnet build src/IProject/IProject.csproj -t:Run
```

Release APK, written to `src/IProject/bin/Release/net9.0-android/publish/`:

```bash
dotnet publish src/IProject/IProject.csproj -c Release
```

## How to use it

| Where | Action |
|---|---|
| Projects | Tap to open a project; long-press to rename or delete it |
| Projects, ⋮ menu | **Back up…** / **Restore…** |
| Project | Tap the circle to advance the status (Done goes back to To do) |
| Quick-add bar | Tap the colored button to switch the type; new tasks go into the tab you're on |
| Task | Changes save as you make them |
| Attachment | Tap to open; long-press to share or delete |
| Comment | Long-press to copy or delete |

## Data and privacy

Everything lives in the app's private storage:

| Path | Contents |
|---|---|
| `iproject.db3` | SQLite database: projects, tasks, checklist items, comments, attachment records |
| `attachments/<task id>/` | Copies of attached files |

The app makes no network requests and never asks for runtime permissions; photos and files are chosen through Android's system pickers. Android's Google Drive auto-backup is turned off (`android:allowBackup="false"`), so nothing is copied to the cloud.

> **Note:** uninstalling the app deletes all its data. Use **Back up…** regularly and keep the .zip somewhere safe.

## Project structure

```
src/IProject/
├── Models/          Entities (Project, TaskItem, ChecklistItem, Attachment, Comment) and enums
├── Services/        Database (sqlite-net) and BackupService (zip export/import)
├── ViewModels/      MVVM view models (CommunityToolkit.Mvvm)
├── Views/           XAML pages, plus PressableGrid (tap, long press, ripple) and SegmentedControl
├── Resources/       App icon, splash screen, fonts, styles
└── Platforms/Android/
```

## Tech stack

- [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) 9, Android only
- [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) for storage
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) for view models
