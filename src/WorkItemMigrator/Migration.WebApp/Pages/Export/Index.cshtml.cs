using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;
using Migration.WebApp.Infrastructure.Services;

namespace Migration.WebApp.Pages.Export;

public class ExportIndexModel : PageModel
{
    private readonly MigrationDbContext _context;
    private readonly IExportService _exportService;

    public ExportIndexModel(MigrationDbContext context, IExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public List<JiraExportRun> RecentExportRuns { get; set; } = new();
    public SelectList OrganizationOptions { get; set; } = new(new List<SelectListItem>(), "Value", "Text");
    public BatchExportPlan? ExportPlan { get; set; }

    [BindProperty]
    public ExportRequest ExportRequest { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreatePlanAsync()
    {
        await LoadDataAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Set current user ID (in real app, get from authentication)
            ExportRequest.UserId = 1;

            ExportPlan = await _exportService.CreateExportPlanAsync(ExportRequest);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Failed to create export plan: {ex.Message}");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmExportAsync()
    {
        try
        {
            ExportRequest.UserId = 1; // Set current user ID
            var exportRunId = await _exportService.StartExportAsync(ExportRequest);
            return RedirectToPage("/Export/Details", new { id = exportRunId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Failed to start export: {ex.Message}");
            await LoadDataAsync();
            return Page();
        }
    }

    private async Task LoadDataAsync()
    {
        // Load organizations
        var organizations = await _context.Organizations
            .Where(o => o.IsActive)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();
        OrganizationOptions = new SelectList(organizations, "Value", "Text");

        // Load recent export runs
        RecentExportRuns = await _context.ExportRuns
            .Include(r => r.Organization)
            .Include(r => r.User)
            .OrderByDescending(r => r.StartedAt)
            .Take(10)
            .ToListAsync();
    }
}