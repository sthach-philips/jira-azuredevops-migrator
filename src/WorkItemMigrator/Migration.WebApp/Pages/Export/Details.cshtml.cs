using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;
using Migration.WebApp.Infrastructure.Services;

namespace Migration.WebApp.Pages.Export;

public class ExportDetailsModel : PageModel
{
    private readonly MigrationDbContext _context;
    private readonly IExportService _exportService;

    public ExportDetailsModel(MigrationDbContext context, IExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public JiraExportRun? ExportRun { get; set; }
    public List<JiraExportedItem> ExportedItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ExportRun = await _context.ExportRuns
            .Include(r => r.Organization)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ExportRun == null)
        {
            return NotFound();
        }

        ExportedItems = await _context.ExportedItems
            .Where(i => i.ExportRunId == id)
            .OrderByDescending(i => i.ExportedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        await _exportService.CancelExportAsync(id);
        return RedirectToPage(new { id });
    }
}