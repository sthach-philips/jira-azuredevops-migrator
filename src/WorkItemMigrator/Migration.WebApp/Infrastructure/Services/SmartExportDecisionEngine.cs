using JiraExport;
using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Infrastructure.Services;

public class SmartExportDecisionEngine
{
    private readonly ItemHashCalculator _hashCalculator;

    public SmartExportDecisionEngine(ItemHashCalculator hashCalculator)
    {
        _hashCalculator = hashCalculator;
    }

    public ExportDecision ShouldExportItem(
        JiraItem jiraItem,
        JiraExportedItem? lastExport,
        string currentConfigHash,
        DeduplicationStrategy strategy = DeduplicationStrategy.Balanced)
    {
        var decision = new ExportDecision { JiraKey = jiraItem.Key };

        // Step 1: New item - always export
        if (lastExport == null)
        {
            decision.ShouldExport = true;
            decision.Reason = ReExportReason.NewItem;
            return decision;
        }

        // Step 2: Check Jira modification date
        var jiraUpdated = GetJiraUpdatedDate(jiraItem);
        if (jiraUpdated > lastExport.JiraUpdatedAt)
        {
            decision.ShouldExport = true;
            decision.Reason = ReExportReason.JiraContentChanged;
            return decision;
        }

        // Step 3: Check configuration changes
        if (lastExport.ExportRun.ConfigurationHash != currentConfigHash)
        {
            decision.ShouldExport = true;
            decision.Reason = ReExportReason.FieldMappingChanged;
            return decision;
        }

        // Step 4: Apply strategy-specific rules
        return strategy switch
        {
            DeduplicationStrategy.Conservative => ApplyConservativeRules(decision),
            DeduplicationStrategy.Aggressive => ApplyAggressiveRules(decision),
            _ => ApplyBalancedRules(decision)
        };
    }

    private ExportDecision ApplyBalancedRules(ExportDecision decision)
    {
        decision.ShouldExport = false;
        decision.Reason = ReExportReason.NoChangesDetected;
        return decision;
    }

    private ExportDecision ApplyConservativeRules(ExportDecision decision)
    {
        // Conservative: re-export on any doubt
        decision.ShouldExport = true;
        decision.Reason = ReExportReason.ForceReExport;
        return decision;
    }

    private ExportDecision ApplyAggressiveRules(ExportDecision decision)
    {
        // Aggressive: skip unless absolutely necessary
        decision.ShouldExport = false;
        decision.Reason = ReExportReason.NoChangesDetected;
        return decision;
    }

    private DateTime GetJiraUpdatedDate(JiraItem jiraItem)
    {
        // Get the latest revision date from the item
        if (jiraItem.Revisions?.Any() == true)
        {
            return jiraItem.Revisions.Max(r => r.Time);
        }

        // Fallback to parsing from RemoteIssue if available
        var updatedToken = jiraItem.RemoteIssue?.SelectToken("$.fields.updated");
        if (updatedToken != null && DateTime.TryParse(updatedToken.ToString(), out var updated))
        {
            return updated;
        }

        return DateTime.MinValue;
    }
}

public enum DeduplicationStrategy
{
    Conservative,
    Balanced,
    Aggressive,
    Custom
}

public enum ReExportReason
{
    NewItem,
    JiraContentChanged,
    FieldMappingAdded,
    FieldMappingChanged,
    FieldMappingRemoved,
    LinkMappingChanged,
    ValueMappingChanged,
    UserMappingChanged,
    RelationshipChanged,
    AttachmentChanged,
    ForceReExport,
    NoChangesDetected
}

public class ExportDecision
{
    public string JiraKey { get; set; } = string.Empty;
    public bool ShouldExport { get; set; }
    public ReExportReason Reason { get; set; }
    public List<string> ChangedFields { get; set; } = new();
    public TimeSpan EstimatedExportTime { get; set; }
}