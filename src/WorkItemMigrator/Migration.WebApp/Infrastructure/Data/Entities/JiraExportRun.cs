namespace Migration.WebApp.Infrastructure.Data.Entities;

public class JiraExportRun
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public string JiraProjectKey { get; set; } = string.Empty;
    public string JiraQuery { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ExportStatus Status { get; set; }
    public int TotalItemsFound { get; set; }
    public int ItemsExported { get; set; }
    public string ConfigurationHash { get; set; } = string.Empty;
    public string ConfigurationSnapshot { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    
    public Organization Organization { get; set; } = null!;
    public MigrationUser User { get; set; } = null!;
    public List<JiraExportedItem> ExportedItems { get; set; } = new();
}

public enum ExportStatus
{
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}