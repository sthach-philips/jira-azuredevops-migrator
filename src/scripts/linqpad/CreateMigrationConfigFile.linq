<Query Kind="Program">
  <Output>DataGrids</Output>
  <NuGetReference>CsvHelper</NuGetReference>
  <NuGetReference>EPPlus</NuGetReference>
  <NuGetReference>EPPlus.Interfaces</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Json</NuGetReference>
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>Microsoft.Extensions.Configuration</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>OfficeOpenXml</Namespace>
  <Namespace>System.ComponentModel</Namespace>
</Query>

void Main()
{
	var (excelFilePath, outputJsonPath) = GetFilePaths("JiraAzureFieldMappings_04112025.xlsx", "config-scrum-philips-autogen.json");
		
	// Retrieve worksheets while keeping ExcelPackage alive
	var typeMappingPackage = GetWorksheet(excelFilePath, "JiraToAzure-Issue");
	var linkMappingPackage = GetWorksheet(excelFilePath, "Jira-IssueNavigationDirection");
	var standardFieldPackage = GetWorksheet(excelFilePath, "JiraToAzure-Fields");
	var customFieldPackage = GetWorksheet(excelFilePath, "JiraToAzureCustomMaps");

	// Create mapping configurations with lambda delegates
	var typeMappingConfig = new MappingConfiguration<TypeMapping>
	{
		RequiredHeaders = new[] { "JiraIssueName", "AzureIssueName" },
		FactoryMethod = (ws, headers, row) => new TypeMapping
		{
			Source = ws.Cells[row, headers["JiraIssueName"]].Text.Trim(),
			Target = ws.Cells[row, headers["AzureIssueName"]].Text.Trim()
		}
	};

	var linkMappingConfig = new MappingConfiguration<LinkMapping>
	{
		RequiredHeaders = new[] { "Source", "Target" },
		FactoryMethod = (ws, headers, row) => new LinkMapping
		{
			Source = ws.Cells[row, headers["Source"]].Text.Trim(),
			Target = ws.Cells[row, headers["Target"]].Text.Trim()
		}
	};

	var fieldMappingConfig = new MappingConfiguration<FieldMapping>
	{
		RequiredHeaders = new[] { "JiraFieldId", "AzureFieldReferenceName", "AzureDevopMigratorMapperType", "JiraFieldDisplayName", "WillMap" },
		FactoryMethod = (ws, headers, row) => new FieldMapping
		{
			Source = ws.Cells[row, headers["JiraFieldId"]].Text.Trim(),
			SourceName = ws.Cells[row, headers["JiraFieldDisplayName"]].Text.Trim(),
			Target = ws.Cells[row, headers["AzureFieldReferenceName"]].Text.Trim(),
			Mapper = ws.Cells[row, headers["AzureDevopMigratorMapperType"]].Text.Trim()
		}
	};

	var customFieldMappingConfig = new MappingConfiguration<CustomFieldMappingRaw>
	{
		RequiredHeaders = new[] { "SourceField", "SourceFieldName", "TargetField", "ValueSource", "ValueTarget", "For" },
		FactoryMethod = (ws, headers, row) => new CustomFieldMappingRaw
		{
			SourceField = ws.Cells[row, headers["SourceField"]].Text.Trim(),
			SourceFieldName = ws.Cells[row, headers["SourceFieldName"]].Text.Trim(),
			TargetField = ws.Cells[row, headers["TargetField"]].Text.Trim(),
			For = headers.ContainsKey("For") ? ws.Cells[row, headers["For"]].Text.Trim() : null,
			ValueSource = ws.Cells[row, headers["ValueSource"]].Text.Trim(),
			ValueTarget = ws.Cells[row, headers["ValueTarget"]].Text.Trim()
		}
	};

	// Retrieve mapped records dynamically using configurations while keeping packages alive
	var typeMappings = GetMappedRecords(typeMappingPackage, typeMappingConfig);
	var linkMappings = GetMappedRecords(linkMappingPackage, linkMappingConfig);
	var standardFieldMappings = GetMappedRecords(standardFieldPackage, fieldMappingConfig);
	var rawCustomMappings = GetMappedRecords(customFieldPackage, customFieldMappingConfig);

	// Process and consolidate custom field mappings
	var customFieldMappings = rawCustomMappings
		.GroupBy(r => new { r.SourceField, r.SourceFieldName, r.TargetField, r.For })
		.Select(group => new CustomFieldMapping
		{
			Source = group.Key.SourceField,
			SourceName = string.IsNullOrEmpty(group.Key.SourceFieldName) ? null : group.Key.SourceFieldName,
			Target = group.Key.TargetField,
			For = string.IsNullOrEmpty(group.Key.For) ? null : group.Key.For,
			Mapping = new MappingContainer
			{
				Values = group.Select(r => new MappingValue { Source = r.ValueSource, Target = r.ValueTarget }).ToList()
			}
		})
		.ToList();

	// Consolidate standard and custom field mappings
	var combinedFieldMappings = ConsolidateFieldMappings(standardFieldMappings, customFieldMappings);

	// Generate final JSON structure
	var jsonObject = CreateJsonObject(typeMappings, combinedFieldMappings, linkMappings);

	// Serialize to JSON preserving original property casing
	var jsonSerializerSettings = new JsonSerializerSettings
	{
		Formatting = Newtonsoft.Json.Formatting.Indented,
		ContractResolver = new DefaultContractResolver { NamingStrategy = new DefaultNamingStrategy() }
	};

	var modifiedJson = JsonConvert.SerializeObject(jsonObject, jsonSerializerSettings);
	File.WriteAllText(outputJsonPath, modifiedJson);
	Console.WriteLine("Modified JSON file generated successfully!");

	// Dispose ExcelPackages to free resources
	if (typeMappingPackage?.Item2 != null)
	{
		typeMappingPackage?.Item2?.Dispose();
	}

	if (linkMappingPackage?.Item2 != null)
	{
		linkMappingPackage?.Item2?.Dispose();
	}

	if (standardFieldPackage?.Item2 != null)
	{
		standardFieldPackage?.Item2?.Dispose();
	}

	if (customFieldPackage?.Item2 != null)
	{
		customFieldPackage?.Item2?.Dispose();
	}
}

// --- Create the JSON object; includes default properties plus the mapped collections. ---
Root CreateJsonObject(List<TypeMapping> typeMappings, List<CombinedFieldMapping> fieldMappings, List<LinkMapping> linkMappings)
{
	return new Root
	{
		AttachmentFolder = "attachments",
		BaseAreaPath = "ECG%20Solutions/Rev Cycle Mgmt",
		BaseIterationPath = "ECG%20Solutions/2025",
		BatchSize = 20,
		DownloadOptions = 7,
		EpicLinkField = "Epic Link",
		FieldMap = new FieldMap { Field = fieldMappings },
		IgnoreEmptyRevisions = false,
		IgnoreFailedLinks = true,
		IncludeJiraCssStyles = false,
		IncludeLinkComments = false,
		LinkMap = new LinkMap { Link = linkMappings },
		LogLevel = "Info",
		ProcessTemplate = "Scrum",
		Query = "Key IN ('MID-17555', 'MID-10846')",
		SleepTimeBetweenRevisionImportMilliseconds = 0,
		SourceProject = "SFIN",
		SprintField = "Sprint",
		SuppressNotifications = false,
		TargetProject = "ECG Solutions",
		TypeMap = new TypeMap { Type = typeMappings },
		UserMappingFile = @"C:\temp\jira-azure-migration\workspace\configuration\user_emails.txt",
		UsingJiraCloud = true,
		Workspace = @"C:\temp\jira-azure-migration\workspace\"
	};
}

(string excelFilePath, string outputJsonPath) GetFilePaths(string mappingFilename, string outputFilename)
{
	var tempDirectory = @"C:\temp\jira-azure-migration\configuration";

	// Ensure temp directory exists
	if (!Directory.Exists(tempDirectory))
	{
		Directory.CreateDirectory(tempDirectory);
	}

	// Set the Excel file path (always looks in temp directory)
	var excelFilePath = Path.Combine(tempDirectory, mappingFilename);

	// Set the JSON output file path (always saves in temp directory)
	var outputJsonPath = Path.Combine(tempDirectory, outputFilename);

	return (excelFilePath, outputJsonPath);
}


// Updated GetWorksheet Method to Keep ExcelPackage Alive
Tuple<ExcelPackage, ExcelWorksheet> GetWorksheet(string filePath, string sheetName)
{
	ExcelPackage.License.SetNonCommercialPersonal("<My Name>");
	var package = new ExcelPackage(new FileInfo(filePath));
	var worksheet = package.Workbook.Worksheets[sheetName];

	if (worksheet == null)
		throw new Exception($"Sheet '{sheetName}' not found in the Excel file.");

	return Tuple.Create(package, worksheet); // Keep the package alive
}

// Generic Record Retrieval Using Delegation
List<T> GetMappedRecords<T>(Tuple<ExcelPackage, ExcelWorksheet> packageTuple, MappingConfiguration<T> config) where T : class
{
	var (package, worksheet) = packageTuple; // Extract package and worksheet

	var headers = GetHeaders(worksheet, config.RequiredHeaders);
	var list = new List<T>();

	var configMappingType = typeof(T);
	var isFieldMapping = (configMappingType == typeof(FieldMapping));

	for (var row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
	{
		if (string.IsNullOrEmpty(worksheet.Cells[row, headers[config.RequiredHeaders[0]]].Text))
		{
			break;
		}

		if (!isFieldMapping)
		{
			list.Add(config.FactoryMethod(worksheet, headers, row));
			continue;
		}

		var willMapField = "WillMap";
		var mapperField = "AzureDevopMigratorMapperType";
		
		var willMapValue = worksheet.Cells[row, headers[willMapField]].Text.Trim();
		if (!ConvertToBoolean(willMapValue))
		{
			continue;// skip processing this record if WillMap is "false"
		}

		var mappingTypeValue = worksheet.Cells[row, headers[mapperField]].Text.Trim();
		if (mappingTypeValue.Contains("CustomConfigurationOption", StringComparison.InvariantCultureIgnoreCase))
		{
			continue; // skip processing this record, it will be handled by the customFieldMapping
		}
		
		list.Add(config.FactoryMethod(worksheet, headers, row));
	}

	return list.Cast<T>().ToList();
}

bool ConvertToBoolean(string value)
{
	if (string.IsNullOrEmpty(value))
		return false; // Treat null or empty values as false

	// Handle "1" as true and "0" as false explicitly
	if (value == "1")
	{
		return true;
	}
	if (value == "0")
	{
		return false;
	}

	// Check standard true/false values
	return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

Dictionary<string, int> GetHeaders(ExcelWorksheet worksheet, string[] requiredHeaders)
{
	var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	if (worksheet.Dimension == null)
	{
		throw new Exception("Worksheet is empty or has no header row.");
	}

	for (var col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
	{
		var headerText = worksheet.Cells[1, col].Text.Trim();
		if (!string.IsNullOrEmpty(headerText))
		{
			headers[headerText] = col;
		}
	}

	// Ensure all required headers exist
	foreach (var req in requiredHeaders)
	{
		if (!headers.ContainsKey(req))
		{
			throw new Exception($"Required header '{req}' not found in worksheet.");
		}
	}

	return headers;
}


// Consolidation Method
List<CombinedFieldMapping> ConsolidateFieldMappings(List<FieldMapping> standardMappings, List<CustomFieldMapping> customMappings)
{
	var combined = new List<CombinedFieldMapping>();

	combined.AddRange(standardMappings.Select(fm => new CombinedFieldMapping
	{
		Source = fm.Source,
		SourceName = fm.SourceName,
		Target = fm.Target,
		Mapper = fm.Mapper
	}));

	combined.AddRange(customMappings.Select(cfm => new CombinedFieldMapping
	{
		Source = cfm.Source,
		SourceName = cfm.SourceName,
		Target = cfm.Target,
		For = string.IsNullOrEmpty(cfm.For) ? null : cfm.For,
		Mapping = cfm.Mapping
	}));

	return combined;
}

// Configuration Class for Delegation-Based Mapping
public class MappingConfiguration<T>
{
	public string[] RequiredHeaders { get; set; }
	public Func<ExcelWorksheet, Dictionary<string, int>, int, T> FactoryMethod { get; set; }
}

#region object models

// --- Type Mapping (Jira Issue Types -> Azure Issue Types) ---
public class TypeMapping
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }
}

// --- Link Mapping (Jira Issue Links) ---
public class LinkMapping
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }
}

// --- Standard Field Mapping (1:1 Direct Mappings) ---
public class FieldMapping
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("source-name")]
	public string SourceName { get; set; }

	public bool Map { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }

	[JsonProperty("mapper", NullValueHandling = NullValueHandling.Ignore)]
	public string Mapper { get; set; }
}

// --- Raw Custom Mapping (Individual Rows Before Grouping) ---
public class CustomFieldMappingRaw
{
	public string SourceField { get; set; }

	public string SourceFieldName { get; set; }

	public string TargetField { get; set; }

	public string For { get; set; } // Optional, applies to specific work item types

	public string ValueSource { get; set; }

	public string ValueTarget { get; set; }
}

// --- Consolidated Custom Mapping with Nested Values ---
public class CustomFieldMapping
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("source-name")]
	public string SourceName { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }

	[JsonProperty("for", NullValueHandling = NullValueHandling.Ignore)]
	public string For { get; set; } // Optional filtering based on work item type

	[JsonProperty("mapping")]
	public MappingContainer Mapping { get; set; }
}

// --- Stores Multiple Source-to-Target Mappings ---
public class MappingContainer
{
	[JsonProperty("values")]
	public List<MappingValue> Values { get; set; }
}

// --- Single Source-to-Target Translation ---
public class MappingValue
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }
}

// --- Consolidated Field Mapping (Handles Both Standard & Custom) ---
public class CombinedFieldMapping
{
	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("source-name")]
	public string SourceName { get; set; }

	[JsonProperty("target")]
	public string Target { get; set; }

	[JsonProperty("mapper", NullValueHandling = NullValueHandling.Ignore)]
	public string Mapper { get; set; }

	[JsonProperty("for", NullValueHandling = NullValueHandling.Ignore)]
	public string For { get; set; }

	[JsonProperty("mapping", NullValueHandling = NullValueHandling.Ignore)]
	public MappingContainer Mapping { get; set; }
}

// --- JSON Root Object ---
public class Root
{
	[JsonProperty("attachment-folder")]
	public string AttachmentFolder { get; set; }

	[JsonProperty("base-area-path")]
	public string BaseAreaPath { get; set; }

	[JsonProperty("base-iteration-path")]
	public string BaseIterationPath { get; set; }

	[JsonProperty("batch-size")]
	public int BatchSize { get; set; }

	[JsonProperty("download-options")]
	public int DownloadOptions { get; set; }

	[JsonProperty("epic-link-field")]
	public string EpicLinkField { get; set; }

	// Consolidated field mappings: contains both standard and custom mappings
	[JsonProperty("field-map")]
	public FieldMap FieldMap { get; set; }

	[JsonProperty("ignore-empty-revisions")]
	public bool IgnoreEmptyRevisions { get; set; }

	[JsonProperty("ignore-failed-links")]
	public bool IgnoreFailedLinks { get; set; }

	[JsonProperty("include-jira-css-styles")]
	public bool IncludeJiraCssStyles { get; set; }

	[JsonProperty("include-link-comments")]
	public bool IncludeLinkComments { get; set; }

	[JsonProperty("link-map")]
	public LinkMap LinkMap { get; set; }

	[JsonProperty("log-level")]
	public string LogLevel { get; set; }

	[JsonProperty("process-template")]
	public string ProcessTemplate { get; set; }

	[JsonProperty("query")]
	public string Query { get; set; }

	[JsonProperty("sleep-time-between-revision-import-milliseconds")]
	public int SleepTimeBetweenRevisionImportMilliseconds { get; set; }

	[JsonProperty("source-project")]
	public string SourceProject { get; set; }

	[JsonProperty("sprint-field")]
	public string SprintField { get; set; }

	[JsonProperty("suppress-notifications")]
	public bool SuppressNotifications { get; set; }

	[JsonProperty("target-project")]
	public string TargetProject { get; set; }

	[JsonProperty("type-map")]
	public TypeMap TypeMap { get; set; }

	[JsonProperty("user-mapping-file")]
	public string UserMappingFile { get; set; }

	[JsonProperty("using-jira-cloud")]
	public bool UsingJiraCloud { get; set; }

	[JsonProperty("workspace")]
	public string Workspace { get; set; }
}

// --- Wrapper Classes for JSON Serialization ---
public class FieldMap
{
	[JsonProperty("field")]
	public List<CombinedFieldMapping> Field { get; set; }
}

public class LinkMap
{
	[JsonProperty("link")]
	public List<LinkMapping> Link { get; set; }
}

public class TypeMap
{
	[JsonProperty("type")]
	public List<TypeMapping> Type { get; set; }
}

#endregion