namespace Migration.WebApp.Infrastructure.Data.Entities;

public class JiraExportedItem
{
    public int Id { get; set; }
    public int ExportRunId { get; set; }
    public string JiraKey { get; set; } = string.Empty;
    public string JiraId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public DateTime JiraUpdatedAt { get; set; }
    public string SerializedWiItem { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string FieldsHash { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public ExportItemStatus Status { get; set; }
    public int RevisionCount { get; set; }
    public DateTime? LastJiraChangeDate { get; set; }
    
    public JiraExportRun ExportRun { get; set; } = null!;
}

public enum ExportItemStatus
{
    Exported,
    Failed,
    Skipped
}