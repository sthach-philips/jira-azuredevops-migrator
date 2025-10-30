using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Pages.Configuration;

public class OrganizationsModel : PageModel
{
    private readonly MigrationDbContext _context;

    public OrganizationsModel(MigrationDbContext context)
    {
        _context = context;
    }

    public List<Organization> Organizations { get; set; } = new();

    [BindProperty]
    public Organization NewOrganization { get; set; } = new();

    public async Task OnGetAsync()
    {
        Organizations = await _context.Organizations
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        NewOrganization.CreatedAt = DateTime.UtcNow;
        NewOrganization.IsActive = true;

        _context.Organizations.Add(NewOrganization);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}