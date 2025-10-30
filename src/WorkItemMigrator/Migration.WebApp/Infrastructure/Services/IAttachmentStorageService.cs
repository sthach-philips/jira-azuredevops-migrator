namespace Migration.WebApp.Infrastructure.Services;

public interface IAttachmentStorageService
{
    Task<string> StoreAttachmentAsync(string fileName, Stream content, string? contentType = null);
    Task<Stream> GetAttachmentAsync(string storageLocation);
    Task<bool> AttachmentExistsAsync(string storageLocation);
    Task DeleteAttachmentAsync(string storageLocation);
    Task<string> CalculateFileHashAsync(Stream content);
}

public enum StorageType
{
    Local,
    S3
}