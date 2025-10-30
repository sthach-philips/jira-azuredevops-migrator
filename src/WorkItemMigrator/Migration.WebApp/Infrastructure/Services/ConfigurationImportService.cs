using Migration.Common.Config;
using Migration.WebApp.Infrastructure.Data;
using Migration.WebApp.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Migration.WebApp.Infrastructure.Services;

public class ConfigurationImportService : IConfigurationImportService
{
    private readonly MigrationDbContext _context;
    private readonly IConfigurationService _configService;

    public ConfigurationImportService(MigrationDbContext context, IConfigurationService configService)
    {
        _context = context;
        _configService = configService;
    }

    public async Task<ImportResult> ImportConfigurationAsync(string configJsonPath, int organizationId)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(configJsonPath);
            var config = JsonConvert.DeserializeObject<ConfigJson>(jsonContent);
            
            if (config == null)
            {
                return new ImportResult { Success = false, Message = "Invalid JSON configuration file" };
            }

            return await ImportConfigurationFromJsonAsync(config, organizationId);
        }
        catch (Exception ex)
        {
            return new ImportResult 
            { 
                Success = false, 
                Message = "Failed to read configuration file",
                Errors = { ex.Message }
            };
        }
    }

    public async Task<ImportResult> ImportConfigurationFromJsonAsync(ConfigJson config, int organizationId)
    {
        var result = new ImportResult();
        
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Clear existing default mappings for this organization
            var existingMappings = await _context.FieldMappings
                .Where(f => f.TeamId == null && f.ProductId == null)
                .ToListAsync();
            
            _context.FieldMappings.RemoveRange(existingMappings);

            // Import field mappings
            if (config.FieldMap != null)
            {
                foreach (var fieldMap in config.FieldMap)
                {
                    var mapping = new FieldTranslationMapping
                    {
                        JiraFieldId = fieldMap.Source,
                        JiraFieldDisplayName = GetFieldDisplayName(fieldMap.Source),
                        JiraFieldType = "string", // Default, could be enhanced
                        AzureFieldReferenceName = fieldMap.Target,
                        AzureFieldDisplayName = GetAzureFieldDisplayName(fieldMap.Target),
                        AzureDevOpsMigratorMapperType = ParseMapperType(fieldMap.Mapper),
                        ForWorkItemType = fieldMap.For ?? "All",
                        NotForWorkItemType = fieldMap.NotFor,
                        WillMigrate = true
                    };

                    _context.FieldMappings.Add(mapping);
                    result.FieldMappingsImported++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.Message = $"Successfully imported {result.FieldMappingsImported} field mappings";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Import failed";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<ConfigJson> ExportConfigurationAsync(int organizationId, int? teamId = null, int? productId = null)
    {
        return await _configService.GetConfigJsonAsync(organizationId, teamId, productId);
    }

    private string GetFieldDisplayName(string fieldId)
    {
        // Map common field IDs to display names
        return fieldId switch
        {
            "summary" => "Summary",
            "description" => "Description",
            "priority" => "Priority",
            "status" => "Status",
            "assignee" => "Assignee",
            "reporter" => "Reporter",
            _ => fieldId
        };
    }

    private string GetAzureFieldDisplayName(string referenceName)
    {
        return referenceName switch
        {
            "System.Title" => "Title",
            "System.Description" => "Description",
            "Microsoft.VSTS.Common.Priority" => "Priority",
            "System.State" => "State",
            "System.AssignedTo" => "Assigned To",
            "System.CreatedBy" => "Created By",
            _ => referenceName
        };
    }

    private MapperType ParseMapperType(string mapper)
    {
        return Enum.TryParse<MapperType>(mapper, true, out var result) ? result : MapperType.Direct;
    }
}