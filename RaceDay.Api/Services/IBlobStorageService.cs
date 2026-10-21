namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(IFormFile file, string containerName);
}
