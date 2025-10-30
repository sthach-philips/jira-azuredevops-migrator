namespace Migration.WebApp.Infrastructure.Data.Entities;

public class MigrationUser
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Organization Organization { get; set; } = null!;
    public List<UserCredential> Credentials { get; set; } = new();
}