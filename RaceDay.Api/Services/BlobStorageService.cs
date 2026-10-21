using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Azure Blob Storage implementation. The connection string lives in
/// appsettings.json under AzureBlobStorage:ConnectionString
/// ("UseDevelopmentStorage=true" points at the local Azurite emulator).
/// </summary>
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

        // Public read access on blobs so the browser can show banners and profile pictures.
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // A GUID prefix keeps two uploads with the same file name from overwriting each other.
        string safeName = Path.GetFileName(file.FileName).Replace(' ', '-');
        string blobName = $"{Guid.NewGuid()}_{safeName}";

        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString();
    }
}
