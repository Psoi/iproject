# iProject

A personal project and task tracker for Android (.NET MAUI). Everything is stored on the device: no account, no cloud, no sync.

## Build and install

Needs the .NET 9 SDK with the `android` workload.

```
dotnet build src/IProject/IProject.csproj -t:Run          # debug build onto a connected phone or emulator
dotnet publish src/IProject/IProject.csproj -c Release    # signed APK in bin/Release/net9.0-android/publish
```

## Using it

- **Projects**: tap to open, long-press to rename or delete. Projects whose tasks are all done move to the bottom.
- **Project**: To do / In progress / Done tabs. Tap the circle on a task to move it to the next status
  (Done goes back to To do). The bar at the bottom adds a task to the current tab; tap the colored button to switch
  between Task, Bug and Feature.
- **Task**: changes save as you make them. Long-press an attachment or comment for more options.

## Where data lives

The app's private storage (`FileSystem.AppDataDirectory`):

- `iproject.db3`: SQLite database (projects, tasks, checklist items, comments, attachment records)
- `attachments/<task id>/`: copies of attached files

Android's Google Drive auto-backup is turned off (`allowBackup="false"`). Uninstalling the app deletes the data,
so use **⋮ → Back up…** on the Projects screen now and then. It makes a .zip you can save anywhere through the
share sheet, and **Restore…** loads one back.
