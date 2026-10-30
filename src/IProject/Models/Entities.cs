using SQLite;

namespace IProject.Models;

[Table("projects")]
public class Project
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[Table("tasks")]
public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ProjectId { get; set; }

    public string Title { get; set; } = "";

    public TaskType Type { get; set; }

    public TaskState Status { get; set; }

    public Priority Priority { get; set; } = Priority.Normal;

    /// <summary>Date only; the time part is always midnight.</summary>
    public DateTime? DueDate { get; set; }

    public string Description { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public bool IsOverdue => DueDate is { } due && due.Date < DateTime.Today && Status != TaskState.Done;
}

[Table("checklist_items")]
public class ChecklistItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TaskId { get; set; }

    public string Text { get; set; } = "";

    public bool IsDone { get; set; }

    public int SortOrder { get; set; }
}

[Table("attachments")]
public class Attachment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TaskId { get; set; }

    /// <summary>The name the file had when it was picked.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Path relative to the attachments folder, so a restored backup still resolves.</summary>
    public string StoredName { get; set; } = "";

    public long Size { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string FullPath { get; set; } = "";

    [Ignore]
    public bool IsImage => Path.GetExtension(FileName).ToLowerInvariant()
        is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";

    [Ignore]
    public string Details => $"{Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant()} · {FormatSize(Size)}";

    static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.0} MB",
    };
}

[Table("comments")]
public class Comment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TaskId { get; set; }

    public string Text { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string Timestamp => CreatedAt.ToString("d MMM yyyy, HH:mm");
}
