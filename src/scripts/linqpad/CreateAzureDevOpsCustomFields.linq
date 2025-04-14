<Query Kind="Program">
  <NuGetReference>CsvHelper</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>System.Json</NuGetReference>
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>CsvHelper</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>RestSharp</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

void Main()
{
    string organization = Util.GetPassword("azure.org"); // Azure DevOps Org Name
    string pat = Util.GetPassword("azure.pat"); // Securely retrieve PAT
    string csvFileName = "sample_fields_03.csv"; // Name of the CSV file containing field data
    string apiVersion = "7.1"; // Specify the Azure DevOps API version here

    // Initialize the RestClient
    var client = InitializeClient(organization, pat);

    // Read fields from CSV
    string scriptDirectory = Path.GetDirectoryName(Util.CurrentQueryPath);
    string csvPath = Path.Combine(scriptDirectory, csvFileName);
    var fieldsToCreate = ReadCsv(csvPath);

    // Retrieve existing fields
    var existingFields = GetExistingFields(client, apiVersion);

    // Process the fields
    ProcessFields(fieldsToCreate, existingFields, client, apiVersion);
}

// Initialize RestClient with necessary headers
RestClient InitializeClient(string organization, string pat)
{
    var client = new RestClient($"https://dev.azure.com/{organization}/");
    client.AddDefaultHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"))}");
    client.AddDefaultHeader("Accept", "application/json");
    return client;
}

// Retrieve existing fields in Azure DevOps
List<ExistingField> GetExistingFields(RestClient client, string apiVersion)
{
    string url = $"_apis/wit/fields?api-version={apiVersion}"; // Use the specified API version
    var request = new RestRequest(url, Method.Get);
    var response = client.Execute(request);

    if (!response.IsSuccessful)
    {
        Console.WriteLine($"Error retrieving existing fields: {response.StatusCode} - {response.StatusDescription}");
        return new List<ExistingField>();
    }

    var data = JsonConvert.DeserializeObject<ExistingFieldsResponse>(response.Content);
    return data.Value;
}

// Process fields (create, handle duplicates, log results)
void ProcessFields(List<FieldDefinition> fieldsToCreate, List<ExistingField> existingFields, RestClient client, string apiVersion)
{
    var successfulCreations = new List<string>();
    var duplicateFields = new List<string>();
    var failedCreations = new List<(string FieldName, string Reason)>();

    foreach (var field in fieldsToCreate)
    {
        try
        {
            if (existingFields.Any(f => f.ReferenceName.Equals(field.ReferenceName, StringComparison.OrdinalIgnoreCase)))
            {
                duplicateFields.Add(field.Name);
                continue; // Skip adding duplicate fields
            }

            string url = $"_apis/wit/fields?api-version={apiVersion}"; // Use the specified API version

            var request = new RestRequest(url, Method.Post);
            request.AddJsonBody(new
            {
                name = field.Name,
                referenceName = field.ReferenceName,
                type = field.Type,
                description = field.Description
            });

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                successfulCreations.Add(field.Name);
            }
            else
            {
                failedCreations.Add((field.Name, $"{response.StatusCode} - {response.StatusDescription}: {response.Content}"));
            }
		}
		catch (Exception ex)
		{
			failedCreations.Add((field.Name, ex.Message));
		}
	}

	successfulCreations.Dump("Successfully Created Fields");
	duplicateFields.Dump("Fields That Were Duplicates and Skipped");
	failedCreations.Dump("Fields That Failed (With Reasons)");
}

// Function to read CSV using CsvHelper
List<FieldDefinition> ReadCsv(string filePath)
{
	using (var reader = new StreamReader(filePath))
	using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
	{
		return csv.GetRecords<FieldDefinition>().ToList();
	}
}

// Static class for Azure DevOps field types
public static class FieldTypes
{
	public const string String = "string";
	public const string Integer = "integer";
	public const string DateTime = "dateTime";
	public const string PlainText = "plainText";
	public const string Html = "html";
	public const string TreePath = "treePath";
	public const string Boolean = "boolean";
	public const string Double = "double";
	public const string Guid = "guid";
	public const string History = "history";
}

public class FieldDefinition
{
	public string Name { get; set; }
	public string ReferenceName { get; set; }
	public string Type { get; set; }
	public string Description { get; set; }
}

public class ExistingFieldsResponse
{
	public List<ExistingField> Value { get; set; }
}

public class ExistingField
{
	public string Name { get; set; }
	public string ReferenceName { get; set; }
}
