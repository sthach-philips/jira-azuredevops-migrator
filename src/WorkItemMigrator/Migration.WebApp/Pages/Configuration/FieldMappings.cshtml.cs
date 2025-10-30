using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Pages.Configuration;

public class FieldMappingsModel : PageModel
{
    private readonly MigrationDbContext _context;

    public FieldMappingsModel(MigrationDbContext context)
    {
        _context = context;
    }

    public List<FieldTranslationMapping> FieldMappings { get; set; } = new();
    public SelectList OrganizationOptions { get; set; } = new(new List<SelectListItem>(), "Value", "Text");

    [BindProperty(SupportsGet = true)]
    public int? SelectedOrganizationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ViewLevel { get; set; } = "Default";

    public async Task OnGetAsync()
    {
        // Load organizations for dropdown
        var organizations = await _context.Organizations
            .Where(o => o.IsActive)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();
        OrganizationOptions = new SelectList(organizations, "Value", "Text", SelectedOrganizationId?.ToString());

        if (SelectedOrganizationId.HasValue)
        {
            await LoadFieldMappingsAsync();
        }
    }

    private async Task LoadFieldMappingsAsync()
    {
        var query = _context.FieldMappings.AsQueryable();

        switch (ViewLevel)
        {
            case "Default":
                query = query.Where(f => f.TeamId == null && f.ProductId == null);
                break;
            case "Team":
                query = query.Where(f => f.TeamId != null && f.ProductId == null);
                break;
            case "Product":
                query = query.Where(f => f.ProductId != null);
                break;
        }

        FieldMappings = await query
            .OrderBy(f => f.JiraFieldDisplayName)
            .ToListAsync();
    }
}