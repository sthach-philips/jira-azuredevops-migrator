using JiraExport;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Migration.WIContract;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;
using Migration.WebApp.Shared;
using Newtonsoft.Json;

namespace Migration.WebApp.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly MigrationDbContext _context;
    private readonly IConfigurationService _configService;
    private readonly SmartExportDecisionEngine _decisionEngine;
    private readonly ItemHashCalculator _hashCalculator;
    private readonly IHubContext<MigrationProgressHub> _hubContext;

    public ExportService(
        MigrationDbContext context,
        IConfigurationService configService,
        SmartExportDecisionEngine decisionEngine,
        ItemHashCalculator hashCalculator,
        IHubContext<MigrationProgressHub> hubContext)
    {
        _context = context;
        _configService = configService;
        _decisionEngine = decisionEngine;
        _hashCalculator = hashCalculator;
        _hubContext = hubContext;
    }

    public async Task<int> StartExportAsync(ExportRequest request)
    {
        var configHash = await _configService.CalculateConfigurationHashAsync(request.OrganizationId);
        var config = await _configService.GetConfigJsonAsync(request.OrganizationId);

        var exportRun = new JiraExportRun
        {
            OrganizationId = request.OrganizationId,
            UserId = request.UserId,
            JiraProjectKey = request.JiraProjectKey,
            JiraQuery = request.JiraQuery,
            StartedAt = DateTime.UtcNow,
            Status = ExportStatus.Running,
            ConfigurationHash = configHash,
            ConfigurationSnapshot = JsonConvert.SerializeObject(config)
        };

        _context.ExportRuns.Add(exportRun);
        await _context.SaveChangesAsync();

        // Start background export process
        _ = Task.Run(() => ProcessExportAsync(exportRun.Id, request));

        return exportRun.Id;
    }

    public async Task<ExportStatus> GetExportStatusAsync(int exportRunId)
    {
        var exportRun = await _context.ExportRuns.FindAsync(exportRunId);
        return exportRun?.Status ?? ExportStatus.Failed;
    }

    public async Task<BatchExportPlan> CreateExportPlanAsync(ExportRequest request)
    {
        var plan = new BatchExportPlan();
        var configHash = await _configService.CalculateConfigurationHashAsync(request.OrganizationId);

        // Get existing exports for comparison
        var existingExports = await _context.ExportedItems
            .Where(e => e.ExportRun.OrganizationId == request.OrganizationId)
            .GroupBy(e => e.JiraKey)
            .Select(g => g.OrderByDescending(e => e.ExportedAt).First())
            .ToDictionaryAsync(e => e.JiraKey);

        // Simulate Jira query results (in real implementation, this would call Jira API)
        var jiraItems = await SimulateJiraQueryAsync(request.JiraQuery);

        foreach (var jiraItem in jiraItems)
        {
            existingExports.TryGetValue(jiraItem.Key, out var lastExport);
            var decision = _decisionEngine.ShouldExportItem(jiraItem, lastExport, configHash, request.Strategy);

            if (decision.ShouldExport || request.ForceReExport)
            {
                plan.ItemsToExport.Add(decision);
            }
            else
            {
                plan.ItemsToSkip.Add(decision);
            }
        }

        plan.TotalEstimatedTime = TimeSpan.FromSeconds(plan.ItemsToExport.Count * 2); // 2 seconds per item estimate

        return plan;
    }

    public async Task CancelExportAsync(int exportRunId)
    {
        var exportRun = await _context.ExportRuns.FindAsync(exportRunId);
        if (exportRun != null)
        {
            exportRun.Status = ExportStatus.Cancelled;
            exportRun.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task ProcessExportAsync(int exportRunId, ExportRequest request)
    {
        try
        {
            var exportRun = await _context.ExportRuns.FindAsync(exportRunId);
            if (exportRun == null) return;

            var plan = await CreateExportPlanAsync(request);
            exportRun.TotalItemsFound = plan.ItemsToExport.Count + plan.ItemsToSkip.Count;
            await _context.SaveChangesAsync();

            var processed = 0;
            foreach (var decision in plan.ItemsToExport)
            {
                if (exportRun.Status == ExportStatus.Cancelled) break;

                await ProcessSingleItemAsync(exportRunId, decision);
                processed++;

                // Send progress update
                var progress = new { 
                    Percentage = (processed * 100) / plan.ItemsToExport.Count,
                    Message = $"Processed {processed} of {plan.ItemsToExport.Count} items"
                };
                await _hubContext.Clients.Group($"export-{exportRunId}").SendAsync("ExportProgress", progress);
            }

            exportRun.Status = ExportStatus.Completed;
            exportRun.CompletedAt = DateTime.UtcNow;
            exportRun.ItemsExported = processed;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var exportRun = await _context.ExportRuns.FindAsync(exportRunId);
            if (exportRun != null)
            {
                exportRun.Status = ExportStatus.Failed;
                exportRun.ErrorDetails = ex.Message;
                exportRun.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessSingleItemAsync(int exportRunId, ExportDecision decision)
    {
        // Simulate item processing (in real implementation, this would use JiraMapper)
        var wiItem = new WiItem
        {
            OriginId = decision.JiraKey,
            Type = "Story", // Would be determined from Jira
            Revisions = new List<WiRevision>()
        };

        var contentHash = _hashCalculator.CalculateContentHash(wiItem);
        var serializedItem = JsonConvert.SerializeObject(wiItem);

        var exportedItem = new JiraExportedItem
        {
            ExportRunId = exportRunId,
            JiraKey = decision.JiraKey,
            JiraId = decision.JiraKey, // Would be actual Jira ID
            ItemType = wiItem.Type,
            JiraUpdatedAt = DateTime.UtcNow,
            SerializedWiItem = serializedItem,
            ContentHash = contentHash,
            FieldsHash = contentHash, // Would be calculated separately
            ExportedAt = DateTime.UtcNow,
            Status = ExportItemStatus.Exported,
            RevisionCount = wiItem.Revisions.Count
        };

        _context.ExportedItems.Add(exportedItem);
        await _context.SaveChangesAsync();
    }

    private async Task<List<JiraItem>> SimulateJiraQueryAsync(string jqlQuery)
    {
        // Simulate Jira API call - in real implementation, this would use JiraProvider
        await Task.Delay(100); // Simulate API delay

        return new List<JiraItem>
        {
            // Mock data for testing
        };
    }
}