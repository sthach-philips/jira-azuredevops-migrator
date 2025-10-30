using Migration.Common.Config;

namespace Migration.WebApp.Infrastructure.Data.Entities;

public class FieldTranslationMapping
{
    public int Id { get; set; }
    public int? TeamId { get; set; }
    public int? ProductId { get; set; }
    
    public string JiraFieldId { get; set; } = string.Empty;
    public string JiraFieldDisplayName { get; set; } = string.Empty;
    public string JiraFieldType { get; set; } = string.Empty;
    public string JiraSystemObjectType { get; set; } = string.Empty;
    
    public string AzureFieldDisplayName { get; set; } = string.Empty;
    public string AzureFieldReferenceName { get; set; } = string.Empty;
    public string AzureFieldType { get; set; } = string.Empty;
    public MapperType AzureDevOpsMigratorMapperType { get; set; }
    
    public string? MultipleSourceFields { get; set; }
    public string ForWorkItemType { get; set; } = "All";
    public string? NotForWorkItemType { get; set; }
    public bool WillMigrate { get; set; } = true;
    
    public Team? Team { get; set; }
    public Product? Product { get; set; }
}