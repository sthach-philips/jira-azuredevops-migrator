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
    string jiraDomain = Util.GetPassword("jira.baseDomain"); // Replace with your Jira domain
    string apiToken = Util.GetPassword("jira.pat"); // Securely retrieve API token
    string email = Util.GetPassword("jira.email"); // Retrieve Jira email securely
    string fieldUrl = $"{jiraDomain}/rest/api/3/field";

    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{apiToken}")));
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var fieldResponse = client.GetAsync(fieldUrl).Result;

        if (fieldResponse.IsSuccessStatusCode)
        {
            var fieldJson = fieldResponse.Content.ReadAsStringAsync().Result;
            var fields = JsonConvert.DeserializeObject<List<JiraField>>(fieldJson);

            fields.Select(f => new
            {
                f.Id,
                f.Name,
                FieldSchemaType = f.Schema?.Type,
                PossibleValuesJson = GetPossibleValues(client, f) // Pass the same authenticated HttpClient
            }).Dump("All Available Jira Fields with Possible Values");
        }
        else
        {
            $"Error retrieving fields: {fieldResponse.StatusCode} - {fieldResponse.ReasonPhrase}".Dump();
        }
    }
}

// Helper function to extract possible values using the same HttpClient
string GetPossibleValues(HttpClient client, JiraField field)
{
    if (field.Schema?.Type != null &&
        (field.Schema.Type.Contains("array") || field.Schema.Type.Contains("list") || field.Schema.Type.Contains("option")))
    {
        string optionsUrl = $"https://biotel.atlassian.net/rest/api/3/field/{field.Id}/context";
        var optionsResponse = client.GetAsync(optionsUrl).Result;

        if (optionsResponse.IsSuccessStatusCode)
        {
            return optionsResponse.Content.ReadAsStringAsync().Result; // Return serialized JSON response
        }
    }

    return ""; // Return empty string if not the right type
}

// Define expected response structure
public class JiraField
{
    public string Id { get; set; }
    public string Name { get; set; }
    public FieldSchema Schema { get; set; }
}

public class FieldSchema
{
	public string Type { get; set; }
}
