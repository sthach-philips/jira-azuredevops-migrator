using Migration.Common.Config;
using Migration.WebApp.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Migration.WebApp.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly MigrationDbContext _context;

    public DatabaseSeeder(MigrationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        if (!await _context.FieldMappings.AnyAsync(f => f.TeamId == null && f.ProductId == null))
        {
            await SeedDefaultFieldMappingsAsync();
        }
    }

    private async Task SeedDefaultFieldMappingsAsync()
    {
        var defaultMappings = new[]
        {
            new FieldTranslationMapping
            {
                JiraFieldId = "summary",
                JiraFieldDisplayName = "Summary",
                JiraFieldType = "string",
                AzureFieldReferenceName = "System.Title",
                AzureFieldDisplayName = "Title",
                AzureDevOpsMigratorMapperType = MapperType.MapTitle,
                ForWorkItemType = "All",
                WillMigrate = true
            },
            new FieldTranslationMapping
            {
                JiraFieldId = "description",
                JiraFieldDisplayName = "Description",
                JiraFieldType = "string",
                AzureFieldReferenceName = "System.Description",
                AzureFieldDisplayName = "Description",
                AzureDevOpsMigratorMapperType = MapperType.MapRendered,
                ForWorkItemType = "All",
                WillMigrate = true
            },
            new FieldTranslationMapping
            {
                JiraFieldId = "priority",
                JiraFieldDisplayName = "Priority",
                JiraFieldType = "option",
                AzureFieldReferenceName = "Microsoft.VSTS.Common.Priority",
                AzureFieldDisplayName = "Priority",
                AzureDevOpsMigratorMapperType = MapperType.MapValue,
                ForWorkItemType = "All",
                WillMigrate = true
            },
            new FieldTranslationMapping
            {
                JiraFieldId = "status",
                JiraFieldDisplayName = "Status",
                JiraFieldType = "status",
                AzureFieldReferenceName = "System.State",
                AzureFieldDisplayName = "State",
                AzureDevOpsMigratorMapperType = MapperType.MapValue,
                ForWorkItemType = "All",
                WillMigrate = true
            },
            new FieldTranslationMapping
            {
                JiraFieldId = "assignee",
                JiraFieldDisplayName = "Assignee",
                JiraFieldType = "user",
                AzureFieldReferenceName = "System.AssignedTo",
                AzureFieldDisplayName = "Assigned To",
                AzureDevOpsMigratorMapperType = MapperType.MapUser,
                ForWorkItemType = "All",
                WillMigrate = true
            },
            new FieldTranslationMapping
            {
                JiraFieldId = "reporter",
                JiraFieldDisplayName = "Reporter",
                JiraFieldType = "user",
                AzureFieldReferenceName = "System.CreatedBy",
                AzureFieldDisplayName = "Created By",
                AzureDevOpsMigratorMapperType = MapperType.MapUser,
                ForWorkItemType = "All",
                WillMigrate = true
            }
        };

        _context.FieldMappings.AddRange(defaultMappings);
        await _context.SaveChangesAsync();
    }
}