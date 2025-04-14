<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <NuGetReference>System.Json</NuGetReference>
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
    string jiraDomain = Util.GetPassword("jira.baseDomain"); // Replace with your Jira domain
    string apiToken = Util.GetPassword("jira.pat"); // Securely retrieve API token
    string email = Util.GetPassword("jira.email"); // Replace with your Jira account email
    string projectUrl = $"{jiraDomain}/rest/api/3/project";
    
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{apiToken}")));
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    var projectResponse = client.GetAsync(projectUrl).Result;
    
    var successfulProjects = new List<string>(); // Track successfully processed projects
    var results = new List<JiraFieldUsage>(); // Store processed field data

    if (projectResponse.IsSuccessStatusCode)
    {
        var projectJson = projectResponse.Content.ReadAsStringAsync().Result;
        var projects = JsonConvert.DeserializeObject<List<JiraProject>>(projectJson);

        foreach (var project in projects)
        {
            try
            {
                string createmetaUrl = $"{jiraDomain}/rest/api/3/issue/createmeta?projectIds={project.Id}&expand=projects.issuetypes.fields";
                var createmetaResponse = client.GetAsync(createmetaUrl).Result;

                if (createmetaResponse.IsSuccessStatusCode)
                {
                    var createmetaJson = createmetaResponse.Content.ReadAsStringAsync().Result;
                    var createmetaData = JsonConvert.DeserializeObject<JiraCreateMetaResponse>(createmetaJson);

                    var projectData = createmetaData.Projects.FirstOrDefault();
                    
                    if (projectData != null)
                    {
                        foreach (var issueType in projectData.IssueTypes)
                        {
                            foreach (var field in issueType.Fields)
                            {
                                results.Add(new JiraFieldUsage
                                {
                                    JiraProject = project.Name,
                                    JiraProjectKey = project.Key,
                                    JiraIssueType = issueType.Name,
                                    JiraFieldId = field.Key,
                                    JiraFieldName = field.Value.Name,
                                    FieldSchemaType = field.Value.Schema?.Type
                                });
                            }
                        }
                    }

                    successfulProjects.Add(project.Name); // Mark project as successfully processed
                }
                else
                {
                    $"Error retrieving fields for project {project.Name}: {createmetaResponse.StatusCode} - {createmetaResponse.ReasonPhrase}".Dump();
                }
            }
            catch (Exception ex)
            {
                $"Exception processing project {project.Name}: {ex.Message}".Dump();
            }
        }

        results.Dump("Processed Fields Per Issue Type, Per Project");
        successfulProjects.Dump("Successfully Processed Projects");
    }
    else
    {
        $"Error retrieving projects: {projectResponse.StatusCode} - {projectResponse.ReasonPhrase}".Dump();
    }
}

// Define expected response structures
public class JiraProject
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Key { get; set; } // Project Key (Prefix)
}

public class JiraCreateMetaResponse
{
    public List<JiraProjectMetadata> Projects { get; set; }
}

public class JiraProjectMetadata
{
    public string Id { get; set; }
	public string Name { get; set; }
	public List<JiraIssueTypeMetadata> IssueTypes { get; set; }
}

public class JiraIssueTypeMetadata
{
	public string Id { get; set; }
	public string Name { get; set; }
	public Dictionary<string, JiraFieldMetadata> Fields { get; set; }
}

public class JiraFieldMetadata
{
	public string Name { get; set; }
	public FieldSchema Schema { get; set; }
}

public class FieldSchema
{
	public string Type { get; set; }
}

public class JiraFieldUsage
{
	public string JiraProject { get; set; }
	public string JiraProjectKey { get; set; }
	public string JiraIssueType { get; set; }
	public string JiraFieldId { get; set; }
	public string JiraFieldName { get; set; }
	public string FieldSchemaType { get; set; }
}
