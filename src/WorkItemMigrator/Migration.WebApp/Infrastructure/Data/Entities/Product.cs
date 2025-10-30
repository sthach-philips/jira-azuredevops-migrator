namespace Migration.WebApp.Infrastructure.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JiraProjectKey { get; set; } = string.Empty;
    public string JiraQuery { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Team Team { get; set; } = null!;
}