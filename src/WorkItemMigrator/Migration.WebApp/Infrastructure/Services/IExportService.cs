using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Infrastructure.Services;

public interface IExportService
{
    Task<int> StartExportAsync(ExportRequest request);
    Task<ExportStatus> GetExportStatusAsync(int exportRunId);
    Task<BatchExportPlan> CreateExportPlanAsync(ExportRequest request);
    Task CancelExportAsync(int exportRunId);
}

public class ExportRequest
{
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public string JiraProjectKey { get; set; } = string.Empty;
    public string JiraQuery { get; set; } = string.Empty;
    public DeduplicationStrategy Strategy { get; set; } = DeduplicationStrategy.Balanced;
    public bool ForceReExport { get; set; }
}

public class BatchExportPlan
{
    public List<ExportDecision> ItemsToExport { get; set; } = new();
    public List<ExportDecision> ItemsToSkip { get; set; } = new();
    public TimeSpan TotalEstimatedTime { get; set; }
    public Dictionary<ReExportReason, int> ReasonBreakdown => 
        ItemsToExport.GroupBy(d => d.Reason).ToDictionary(g => g.Key, g => g.Count());
}