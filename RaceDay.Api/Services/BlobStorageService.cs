using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;

    public BlobStorageService(IConfiguration config)
    {
        _connectionString = config["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "AzureBlobStorage:ConnectionString is missing from appsettings.json.");
    }

    public async Task<string> UploadAsync(IFormFile file, string containerName)
    {
        var containerClient = new BlobContainerClient(_connectionString, containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        string safeName = Path.GetFileName(file.FileName).Replace(' ', '-');
        string blobName = $"{Guid.NewGuid()}_{safeName}";

        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString();
    }
}
