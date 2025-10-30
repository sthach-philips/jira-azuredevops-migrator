using Migration.Common.Config;

namespace Migration.WebApp.Infrastructure.Services;

public interface IConfigurationImportService
{
    Task<ImportResult> ImportConfigurationAsync(string configJsonPath, int organizationId);
    Task<ImportResult> ImportConfigurationFromJsonAsync(ConfigJson config, int organizationId);
    Task<ConfigJson> ExportConfigurationAsync(int organizationId, int? teamId = null, int? productId = null);
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int FieldMappingsImported { get; set; }
    public List<string> Errors { get; set; } = new();
}