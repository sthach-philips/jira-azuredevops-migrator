<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>System.Json</NuGetReference>
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

void Main()
{
   string organization = Util.GetPassword("azure.org"); // Azure DevOps Org Name
    string pat = Util.GetPassword("azure.pat"); // Securely retrieve PAT
    string url = $"https://dev.azure.com/{organization}/_apis/wit/fields?api-version=7.1";

    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));

    var response = client.GetAsync(url).Result;
    if (response.IsSuccessStatusCode)
    {
        var json = response.Content.ReadAsStringAsync().Result;
        var apiResponse = JsonConvert.DeserializeObject<AzureDevOpsFieldsResponse>(json);

        // Transform supported operations into a pipe-separated string
        var formattedFields = apiResponse.Value.Select(f => new
        {
            f.Name,
            f.ReferenceName,
            f.Type,
            f.ReadOnly,
			SupportedOperations = f.SupportedOperations != null
				? string.Join(" | ", f.SupportedOperations.Select(o => o.Name))
				: "None",
			f.Url
		});

		formattedFields.Dump(); // Dumps formatted fields
	}
	else
	{
		$"Error: {response.StatusCode} - {response.ReasonPhrase}".Dump();
	}
}

// Define the expected response structure
public class AzureDevOpsFieldsResponse
{
	public List<Field> Value { get; set; }
}

public class Field
{
	public string Name { get; set; }
	public string ReferenceName { get; set; }
	public string Type { get; set; }
	public bool ReadOnly { get; set; }
	public List<Operation> SupportedOperations { get; set; }
	public string Url { get; set; }
}

// Define supported operations for each field
public class Operation
{
	public string Name { get; set; }
}