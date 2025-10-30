namespace Migration.WebApp.Infrastructure.Data.Entities;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<Team> Teams { get; set; } = new();
    public List<MigrationUser> Users { get; set; } = new();
}