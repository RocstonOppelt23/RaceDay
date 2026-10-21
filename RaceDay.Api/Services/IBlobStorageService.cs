namespace RaceDay.Api.Services;

/// <summary>
/// Stores uploaded files in Azure Blob Storage and returns their public URL.
/// The MVC front end never talks to Azure directly; every upload passes through the API.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Uploads the file into the named container and returns the blob's URL.</summary>
    Task<string> UploadAsync(IFormFile file, string containerName);
}
