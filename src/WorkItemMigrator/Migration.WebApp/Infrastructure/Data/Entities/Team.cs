namespace Migration.WebApp.Infrastructure.Data.Entities;

public class Team
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Organization Organization { get; set; } = null!;
    public List<Product> Products { get; set; } = new();
}