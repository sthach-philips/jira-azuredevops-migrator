using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Services;

namespace Migration.WebApp.Pages.Configuration;

public class ImportModel : PageModel
{
    private readonly MigrationDbContext _context;
    private readonly IConfigurationImportService _importService;

    public ImportModel(MigrationDbContext context, IConfigurationImportService importService)
    {
        _context = context;
        _importService = importService;
    }

    [BindProperty]
    public int OrganizationId { get; set; }

    [BindProperty]
    public IFormFile ConfigFile { get; set; } = null!;

    [BindProperty]
    public bool ReplaceExisting { get; set; }

    public SelectList OrganizationOptions { get; set; } = new(new List<SelectListItem>(), "Value", "Text");
    public string Message { get; set; } = string.Empty;
    public ImportResult? ImportResult { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOrganizationsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOrganizationsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            using var stream = ConfigFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var jsonContent = await reader.ReadToEndAsync();

            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Migration.Common.Config.ConfigJson>(jsonContent);
            if (config == null)
            {
                Message = "Invalid JSON configuration file";
                return Page();
            }

            ImportResult = await _importService.ImportConfigurationFromJsonAsync(config, OrganizationId);
            Message = ImportResult.Message;

            if (ImportResult.Success)
            {
                return RedirectToPage("/Configuration/FieldMappings", new { orgId = OrganizationId });
            }
        }
        catch (Exception ex)
        {
            Message = $"Import failed: {ex.Message}";
        }

        return Page();
    }

    private async Task LoadOrganizationsAsync()
    {
        var organizations = await _context.Organizations
            .Where(o => o.IsActive)
            .Select(o => new SelectListItem { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();
        OrganizationOptions = new SelectList(organizations, "Value", "Text");
    }
}