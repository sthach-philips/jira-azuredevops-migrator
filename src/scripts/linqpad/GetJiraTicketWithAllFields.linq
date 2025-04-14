<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <Namespace>RestSharp</Namespace>
  <Namespace>LINQPad</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

void Main()
{
	string jiraDomain = Util.GetPassword("jira.baseDomain"); // Replace with your Jira domain
	string baseUrl = $"{jiraDomain}/rest/api/3/search";
    string email = Util.GetPassword("jira.email");
    string apiToken = Util.GetPassword("jira.pat");
	
	//Filter Based on Created Date
	string startDateSearchFilter = "created >= '2023-01-01'";
	
    // List of project prefixes to process separately
    string[] projectPrefixes = { "AFT", "BI", "BSD", "DRM", "IAI", "MID", "RNDL", "SAL", "SFIN", "APS", "PMD", "ITD", "DEVOPS", "SQA" };
	
    foreach (string projectPrefix in projectPrefixes)
    {
		string jqlQuery = $"{startDateSearchFilter} AND project in ({projectPrefix}) ORDER BY created DESC";
        ProcessProjectIssues(baseUrl, email, apiToken, projectPrefix, jqlQuery);
    }
}

void ProcessProjectIssues(string baseUrl, string email, string apiToken, string projectPrefix, string jqlQuery)
{
    int maxResults = 100;
    int startAt = 0;
    bool hasMoreIssues = true;
    var allIssues = new List<Issue>();
    int totalIssues = 0;
    Dictionary<string, string> fieldNames = new Dictionary<string, string>();
    HashSet<string> uniqueFields = new HashSet<string>(); // Store unique field names for column headers

    $"Processing issues for project: {projectPrefix}".Dump();
    var progressBar = new Util.ProgressBar($"Processing {projectPrefix} Issues").Dump();

    var client = new RestSharp.RestClient(baseUrl);
    var request = new RestSharp.RestRequest();
    var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
    request.AddHeader("Authorization", $"Basic {auth}");
    request.AddQueryParameter("jql", jqlQuery);
    request.AddQueryParameter("fields", "*all");
    request.AddQueryParameter("expand", "names,schema");
    request.AddQueryParameter("maxResults", maxResults.ToString());

    while (hasMoreIssues)
    {
        request.AddQueryParameter("startAt", startAt.ToString());
        var response = client.Execute(request);

        if (!response.IsSuccessful)
        {
            $"Error processing {projectPrefix}: {response.StatusDescription}\nDetails: {response.Content}".Dump();
            return;
        }

        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<SearchResult>(response.Content);

        if (startAt == 0)
        {
            totalIssues = result.total;
            $"Total issues for {projectPrefix}: {totalIssues}".Dump();
            fieldNames = result.names; // Store display names
        }

        foreach (var issue in result.issues)
        {
            var fieldsDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Newtonsoft.Json.JsonConvert.SerializeObject(issue.fields));

            foreach (var field in fieldsDict.Keys)
            {
                uniqueFields.Add(fieldNames.ContainsKey(field) ? fieldNames[field] : field);
            }

            allIssues.Add(issue);
        }

        double progressPercent = ((startAt + result.issues.Count) / (double)totalIssues) * 100;
        Util.Progress = (int)progressPercent;
        progressBar.Percent = (int)progressPercent;

        startAt += maxResults;
        hasMoreIssues = startAt < totalIssues;
    }


    $"Finished processing {projectPrefix}. Total issues: {allIssues.Count}".Dump();

    // Generate CSV for this project
    string csvFilePath = $"C:\\temp\\{projectPrefix}_jira_data.csv";
    using (var writer = new System.IO.StreamWriter(csvFilePath))
    {
        writer.WriteLine($"Issue Key, Issue Type, {string.Join(",", uniqueFields)}");

        foreach (var issue in allIssues)
        {
            var fieldsDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Newtonsoft.Json.JsonConvert.SerializeObject(issue.fields));

            string issueKey = issue.key;
            string issueType = fieldsDict.ContainsKey("issuetype")
                ? ((JObject)fieldsDict["issuetype"])["name"]?.ToString() ?? "Unknown Issue Type"
                : "Unknown Issue Type";

            var rowValues = new List<string> { issueKey, issueType };

            foreach (var field in uniqueFields)
            {
				string fieldKey = fieldNames.ContainsValue(field) ? fieldNames.FirstOrDefault(x => x.Value == field).Key : field;
				string fieldValue = fieldsDict.ContainsKey(fieldKey) ? Newtonsoft.Json.JsonConvert.SerializeObject(fieldsDict[fieldKey]) : "";

				if (fieldValue == "null" || fieldValue == "[]")
					fieldValue = ""; // Replace null or empty lists with blank spaces

				rowValues.Add(fieldValue);
			}

			writer.WriteLine(string.Join(",", rowValues));
		}
	}

	$"Saved {projectPrefix} data to {csvFilePath}".Dump();
}

// Classes to Deserialize JSON Response
public class SearchResult
{
	public int total { get; set; }
	public List<Issue> issues { get; set; }
	public Dictionary<string, string> names { get; set; } // Stores field display names
}

public class Issue
{
	public string key { get; set; }
	public Dictionary<string, object> fields { get; set; } // Capture all fields dynamically
}
