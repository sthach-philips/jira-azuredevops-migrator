namespace Migration.WebApp.Infrastructure.Data.Entities;

public class UserCredential
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public CredentialType Type { get; set; }
    public string ServiceUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public bool IsValid { get; set; } = true;
    
    public MigrationUser User { get; set; } = null!;
}

public enum CredentialType
{
    JiraCloud,
    JiraServer,
    AzureDevOpsServices,
    AzureDevOpsServer
}