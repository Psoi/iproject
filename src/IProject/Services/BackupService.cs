using System.IO.Compression;

namespace IProject.Services;

/// <summary>
/// Backup and restore as a single .zip (the database plus the attachments folder).
/// Nothing leaves the device unless you choose where to save the file.
/// </summary>
public class BackupService(Database database)
{
    const string DatabaseEntry = "iproject.db3";
    const string AttachmentsPrefix = "attachments/";

    /// <summary>Writes the backup to the cache folder and returns its path.</summary>
    public async Task<string> CreateAsync()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"iproject-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.zip");
        if (File.Exists(path))
            File.Delete(path);

        await database.CloseAsync();
        await Task.Run(() =>
        {
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            if (File.Exists(Database.DatabasePath))
                zip.CreateEntryFromFile(Database.DatabasePath, DatabaseEntry);

            if (Directory.Exists(Database.AttachmentsDirectory))
            {
                foreach (var file in Directory.EnumerateFiles(Database.AttachmentsDirectory, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(Database.AttachmentsDirectory, file).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, AttachmentsPrefix + relative);
                }
            }
        });
        return path;
    }

    /// <summary>Replaces all data with the contents of a backup. Throws if the file isn't one.</summary>
    public async Task RestoreAsync(FileResult file)
    {
        // Copy first: the picked stream may not support seeking, which ZipArchive needs.
        var temp = Path.Combine(FileSystem.CacheDirectory, "restore.zip");
        await using (var source = await file.OpenReadAsync())
        await using (var target = File.Create(temp))
            await source.CopyToAsync(target);

        try
        {
            using (var check = ZipFile.OpenRead(temp))
            {
                if (check.GetEntry(DatabaseEntry) == null)
                    throw new InvalidDataException("This file isn't an iProject backup.");
            }

            await database.CloseAsync();
            await Task.Run(() =>
            {
                if (Directory.Exists(Database.AttachmentsDirectory))
                    Directory.Delete(Database.AttachmentsDirectory, recursive: true);

                using var zip = ZipFile.OpenRead(temp);
                var root = Path.GetFullPath(FileSystem.AppDataDirectory) + Path.DirectorySeparatorChar;
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName != DatabaseEntry && !entry.FullName.StartsWith(AttachmentsPrefix))
                        continue;
                    if (string.IsNullOrEmpty(entry.Name))
                        continue; // folder entry

                    var target = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, entry.FullName));
                    if (!target.StartsWith(root))
                        continue; // "../" in an entry name
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, overwrite: true);
                }
            });
        }
        finally
        {
            File.Delete(temp);
        }
    }
}
