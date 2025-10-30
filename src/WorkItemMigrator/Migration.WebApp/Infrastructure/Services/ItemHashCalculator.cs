using Migration.Common.Config;
using Migration.WIContract;
using Migration.WebApp.Infrastructure.Data.Entities;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Migration.WebApp.Infrastructure.Services;

public class ItemHashCalculator
{
    public string CalculateContentHash(WiItem wiItem)
    {
        var serialized = JsonConvert.SerializeObject(wiItem, Formatting.None);
        return ComputeSHA256(serialized);
    }

    public string CalculateFieldsHash(WiItem wiItem, FieldTranslationMapping[] mappings)
    {
        var mappedFields = ExtractMappedFields(wiItem, mappings);
        var serialized = JsonConvert.SerializeObject(mappedFields, Formatting.None);
        return ComputeSHA256(serialized);
    }

    public string CalculateConfigHash(ConfigJson config, string itemType)
    {
        var relevantConfig = ExtractRelevantConfig(config, itemType);
        var serialized = JsonConvert.SerializeObject(relevantConfig, Formatting.None);
        return ComputeSHA256(serialized);
    }

    public string CalculateRelationshipHash(WiItem wiItem)
    {
        var relationships = ExtractRelationships(wiItem);
        var serialized = JsonConvert.SerializeObject(relationships, Formatting.None);
        return ComputeSHA256(serialized);
    }

    private Dictionary<string, object> ExtractMappedFields(WiItem wiItem, FieldTranslationMapping[] mappings)
    {
        var mappedFields = new Dictionary<string, object>();
        var mappingDict = mappings.ToDictionary(m => m.JiraFieldId, m => m);

        foreach (var revision in wiItem.Revisions)
        {
            foreach (var field in revision.Fields)
            {
                if (mappingDict.ContainsKey(field.Key) && mappingDict[field.Key].WillMigrate)
                {
                    mappedFields[field.Key] = field.Value;
                }
            }
        }

        return mappedFields;
    }

    private object ExtractRelevantConfig(ConfigJson config, string itemType)
    {
        return new
        {
            FieldMappings = config.FieldMap?.Where(f => 
                f.For == null || f.For == "All" || f.For == itemType),
            TypeMappings = config.TypeMap?.Where(t => t.Source == itemType),
            LinkMappings = config.LinkMap
        };
    }

    private object ExtractRelationships(WiItem wiItem)
    {
        var relationships = new List<object>();

        foreach (var revision in wiItem.Revisions)
        {
            if (revision.Links != null)
            {
                relationships.AddRange(revision.Links.Select(l => new
                {
                    l.SourceOriginId,
                    l.TargetOriginId,
                    l.WiType,
                    l.Change
                }));
            }
        }

        return relationships;
    }

    private string ComputeSHA256(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }
}