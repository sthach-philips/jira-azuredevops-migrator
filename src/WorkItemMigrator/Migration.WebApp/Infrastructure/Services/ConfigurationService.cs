using Microsoft.EntityFrameworkCore;
using Migration.Common.Config;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Migration.WebApp.Infrastructure.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly MigrationDbContext _context;

    public ConfigurationService(MigrationDbContext context)
    {
        _context = context;
    }

    public async Task<ConfigJson> GetConfigJsonAsync(int organizationId, int? teamId = null, int? productId = null)
    {
        var fieldMappings = await ResolveFieldMappingsAsync(organizationId, teamId, productId);
        
        return new ConfigJson
        {
            FieldMap = fieldMappings.Select(f => new FieldMap
            {
                Source = f.JiraFieldId,
                Target = f.AzureFieldReferenceName,
                Mapper = f.AzureDevOpsMigratorMapperType.ToString(),
                For = f.ForWorkItemType != "All" ? f.ForWorkItemType : null,
                NotFor = f.NotForWorkItemType
            }).ToArray()
        };
    }

    public async Task<FieldTranslationMapping[]> ResolveFieldMappingsAsync(int organizationId, int? teamId = null, int? productId = null)
    {
        // Get all mappings that could apply (default, team, product)
        var query = _context.FieldMappings.Where(f => 
            (f.TeamId == null && f.ProductId == null) ||  // Default mappings
            (f.TeamId == teamId && f.ProductId == null) || // Team overrides
            (f.ProductId == productId));                   // Product overrides

        var allMappings = await query.ToListAsync();

        // Group by field and apply override logic
        var resolvedMappings = allMappings
            .GroupBy(f => f.JiraFieldId)
            .Select(group =>
            {
                // Priority: Product > Team > Default
                return group
                    .OrderByDescending(f => f.ProductId.HasValue ? 2 : f.TeamId.HasValue ? 1 : 0)
                    .First();
            })
            .ToArray();

        return resolvedMappings;
    }

    public async Task<string> CalculateConfigurationHashAsync(int organizationId, int? teamId = null, int? productId = null)
    {
        var config = await GetConfigJsonAsync(organizationId, teamId, productId);
        var serialized = JsonConvert.SerializeObject(config, Formatting.None);
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(hashBytes);
    }
}