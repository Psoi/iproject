using IProject.Models;
using SQLite;

namespace IProject.Services;

/// <summary>
/// All app data: one SQLite file plus an attachments folder, both in the app's private storage.
/// </summary>
public class Database
{
    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, "iproject.db3");
    public static string AttachmentsDirectory => Path.Combine(FileSystem.AppDataDirectory, "attachments");

    SQLiteAsyncConnection? connection;
    readonly SemaphoreSlim initLock = new(1, 1);

    async Task<SQLiteAsyncConnection> Db()
    {
        if (connection != null)
            return connection;

        await initLock.WaitAsync();
        try
        {
            if (connection == null)
            {
                var db = new SQLiteAsyncConnection(DatabasePath);
                await db.CreateTablesAsync(CreateFlags.None,
                    typeof(Project), typeof(TaskItem), typeof(ChecklistItem), typeof(Attachment), typeof(Comment));
                connection = db;
            }
            return connection;
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <summary>Closes the file so a backup can be copied or replaced; the next call reopens it.</summary>
    public async Task CloseAsync()
    {
        if (connection == null)
            return;
        await connection.CloseAsync();
        connection = null;
    }

    // ---- Projects ----

    /// <summary>Unfinished projects first, each group oldest first.</summary>
    public async Task<List<ProjectSummary>> GetProjectSummariesAsync()
    {
        await WaitForTaskSave();
        var db = await Db();
        var projects = await db.Table<Project>().ToListAsync();
        var counts = await db.QueryAsync<StatusCount>(
            "SELECT ProjectId, Status, COUNT(*) AS Count FROM tasks GROUP BY ProjectId, Status");

        return projects
            .Select(p =>
            {
                var mine = counts.Where(c => c.ProjectId == p.Id).ToList();
                return new ProjectSummary(p, mine.Sum(c => c.Count),
                    mine.Where(c => c.Status == TaskState.Done).Sum(c => c.Count));
            })
            .OrderBy(s => s.IsFinished)
            .ThenBy(s => s.Project.CreatedAt)
            .ToList();
    }

    public async Task<Project?> GetProjectAsync(int id) =>
        await (await Db()).FindAsync<Project>(id);

    public async Task SaveProjectAsync(Project project)
    {
        var db = await Db();
        if (project.Id == 0)
            await db.InsertAsync(project);
        else
            await db.UpdateAsync(project);
    }

    public async Task DeleteProjectAsync(Project project)
    {
        var db = await Db();
        foreach (var task in await db.Table<TaskItem>().Where(t => t.ProjectId == project.Id).ToListAsync())
            await DeleteTaskAsync(task);
        await db.DeleteAsync(project);
    }

    // ---- Tasks ----

    /// <summary>Highest priority first, then earliest due date (no date last), then oldest.</summary>
    public async Task<List<TaskItem>> GetTasksAsync(int projectId)
    {
        await WaitForTaskSave();
        var tasks = await (await Db()).Table<TaskItem>().Where(t => t.ProjectId == projectId).ToListAsync();
        return tasks
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.CreatedAt)
            .ToList();
    }

    public async Task<TaskItem?> GetTaskAsync(int id) =>
        await (await Db()).FindAsync<TaskItem>(id);

    public Task SaveTaskAsync(TaskItem task)
    {
        // Leaving a task page saves it while the list underneath is already reloading;
        // the list reads wait for this so they never show the old values.
        lastTaskSave = Save(task);
        return lastTaskSave;
    }

    Task lastTaskSave = Task.CompletedTask;

    async Task Save(TaskItem task)
    {
        var db = await Db();
        task.UpdatedAt = DateTime.Now;
        if (task.Id == 0)
            await db.InsertAsync(task);
        else
            await db.UpdateAsync(task);
    }

    async Task WaitForTaskSave()
    {
        try
        {
            await lastTaskSave;
        }
        catch
        {
            // The saver reports its own failure; a read shouldn't fail because of it.
        }
    }

    public async Task DeleteTaskAsync(TaskItem task)
    {
        var db = await Db();
        await db.ExecuteAsync("DELETE FROM checklist_items WHERE TaskId = ?", task.Id);
        await db.ExecuteAsync("DELETE FROM comments WHERE TaskId = ?", task.Id);
        await db.ExecuteAsync("DELETE FROM attachments WHERE TaskId = ?", task.Id);
        await db.DeleteAsync(task);

        var folder = Path.Combine(AttachmentsDirectory, task.Id.ToString());
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    /// <summary>Checklist progress per task in a project, for the task rows.</summary>
    public async Task<Dictionary<int, (int Done, int Total)>> GetChecklistProgressAsync(int projectId)
    {
        var rows = await (await Db()).QueryAsync<ChecklistCount>(
            "SELECT c.TaskId, SUM(c.IsDone) AS Done, COUNT(*) AS Total FROM checklist_items c " +
            "JOIN tasks t ON t.Id = c.TaskId WHERE t.ProjectId = ? GROUP BY c.TaskId", projectId);
        return rows.ToDictionary(r => r.TaskId, r => (r.Done, r.Total));
    }

    // ---- Checklist ----

    public async Task<List<ChecklistItem>> GetChecklistAsync(int taskId) =>
        await (await Db()).Table<ChecklistItem>().Where(c => c.TaskId == taskId).OrderBy(c => c.SortOrder).ToListAsync();

    public async Task SaveChecklistItemAsync(ChecklistItem item)
    {
        var db = await Db();
        if (item.Id == 0)
        {
            item.SortOrder = await db.ExecuteScalarAsync<int>(
                "SELECT COALESCE(MAX(SortOrder), 0) + 1 FROM checklist_items WHERE TaskId = ?", item.TaskId);
            await db.InsertAsync(item);
        }
        else
        {
            await db.UpdateAsync(item);
        }
    }

    public async Task DeleteChecklistItemAsync(ChecklistItem item) =>
        await (await Db()).DeleteAsync(item);

    // ---- Comments ----

    public async Task<List<Comment>> GetCommentsAsync(int taskId) =>
        await (await Db()).Table<Comment>().Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt).ToListAsync();

    public async Task AddCommentAsync(Comment comment) =>
        await (await Db()).InsertAsync(comment);

    public async Task DeleteCommentAsync(Comment comment) =>
        await (await Db()).DeleteAsync(comment);

    // ---- Attachments ----

    public async Task<List<Attachment>> GetAttachmentsAsync(int taskId)
    {
        var list = await (await Db()).Table<Attachment>().Where(a => a.TaskId == taskId).OrderBy(a => a.AddedAt).ToListAsync();
        foreach (var a in list)
            a.FullPath = Path.Combine(AttachmentsDirectory, a.StoredName);
        return list;
    }

    /// <summary>Copies a picked file into app storage and records it.</summary>
    public async Task<Attachment> AddAttachmentAsync(int taskId, FileResult file)
    {
        Directory.CreateDirectory(Path.Combine(AttachmentsDirectory, taskId.ToString()));

        // A short unique prefix keeps two files with the same name apart.
        var storedName = Path.Combine(taskId.ToString(), Guid.NewGuid().ToString("N")[..8] + "_" + SafeName(file.FileName));
        var fullPath = Path.Combine(AttachmentsDirectory, storedName);

        await using (var source = await file.OpenReadAsync())
        await using (var target = File.Create(fullPath))
            await source.CopyToAsync(target);

        var attachment = new Attachment
        {
            TaskId = taskId,
            FileName = file.FileName,
            StoredName = storedName,
            Size = new FileInfo(fullPath).Length,
            FullPath = fullPath,
        };
        await (await Db()).InsertAsync(attachment);
        return attachment;
    }

    public async Task DeleteAttachmentAsync(Attachment attachment)
    {
        await (await Db()).DeleteAsync(attachment);
        var path = Path.Combine(AttachmentsDirectory, attachment.StoredName);
        if (File.Exists(path))
            File.Delete(path);
    }

    static string SafeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

    class StatusCount
    {
        public int ProjectId { get; set; }
        public TaskState Status { get; set; }
        public int Count { get; set; }
    }

    class ChecklistCount
    {
        public int TaskId { get; set; }
        public int Done { get; set; }
        public int Total { get; set; }
    }
}

public record ProjectSummary(Project Project, int Total, int Done)
{
    public string Name => Project.Name;

    public bool IsFinished => Total > 0 && Done == Total;

    public double Progress => Total == 0 ? 0 : (double)Done / Total;

    public string ProgressText => Total == 0
        ? "No tasks yet"
        : $"{Done} of {Total} done · {(int)Math.Round(Progress * 100)}%";
}
