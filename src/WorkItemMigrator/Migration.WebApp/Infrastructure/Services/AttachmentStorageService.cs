using System.Security.Cryptography;

namespace Migration.WebApp.Infrastructure.Services;

public class AttachmentStorageService : IAttachmentStorageService
{
    private readonly IConfiguration _configuration;
    private readonly StorageType _storageType;
    private readonly string _localPath;

    public AttachmentStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _storageType = Enum.Parse<StorageType>(_configuration["AttachmentStorage:StorageType"] ?? "Local");
        _localPath = _configuration["AttachmentStorage:LocalPath"] ?? "./attachments";
        
        if (_storageType == StorageType.Local && !Directory.Exists(_localPath))
        {
            Directory.CreateDirectory(_localPath);
        }
    }

    public async Task<string> StoreAttachmentAsync(string fileName, Stream content, string? contentType = null)
    {
        var fileHash = await CalculateFileHashAsync(content);
        content.Position = 0;
        
        if (_storageType == StorageType.Local)
        {
            var filePath = Path.Combine(_localPath, $"{fileHash}_{fileName}");
            using var fileStream = File.Create(filePath);
            await content.CopyToAsync(fileStream);
            return filePath;
        }
        
        throw new NotImplementedException("S3 storage not yet implemented");
    }

    public async Task<Stream> GetAttachmentAsync(string storageLocation)
    {
        if (_storageType == StorageType.Local)
        {
            return File.OpenRead(storageLocation);
        }
        
        throw new NotImplementedException("S3 storage not yet implemented");
    }

    public async Task<bool> AttachmentExistsAsync(string storageLocation)
    {
        if (_storageType == StorageType.Local)
        {
            return File.Exists(storageLocation);
        }
        
        throw new NotImplementedException("S3 storage not yet implemented");
    }

    public async Task DeleteAttachmentAsync(string storageLocation)
    {
        if (_storageType == StorageType.Local)
        {
            if (File.Exists(storageLocation))
            {
                File.Delete(storageLocation);
            }
            return;
        }
        
        throw new NotImplementedException("S3 storage not yet implemented");
    }

    public async Task<string> CalculateFileHashAsync(Stream content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content);
        return Convert.ToHexString(hashBytes);
    }
}