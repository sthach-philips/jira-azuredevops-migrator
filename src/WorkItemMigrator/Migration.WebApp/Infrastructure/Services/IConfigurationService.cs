using Migration.Common.Config;
using Migration.WebApp.Infrastructure.Data.Entities;

namespace Migration.WebApp.Infrastructure.Services;

public interface IConfigurationService
{
    Task<ConfigJson> GetConfigJsonAsync(int organizationId, int? teamId = null, int? productId = null);
    Task<FieldTranslationMapping[]> ResolveFieldMappingsAsync(int organizationId, int? teamId = null, int? productId = null);
    Task<string> CalculateConfigurationHashAsync(int organizationId, int? teamId = null, int? productId = null);
}