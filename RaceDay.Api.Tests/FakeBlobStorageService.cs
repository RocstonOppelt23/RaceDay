using Microsoft.AspNetCore.Http;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class FakeBlobStorageService : IBlobStorageService
{
    public List<(string Container, string FileName)> Uploads { get; } = new();

    public Task<string> UploadAsync(IFormFile file, string containerName)
    {
        Uploads.Add((containerName, file.FileName));
        return Task.FromResult($"https://fake.blob.core.windows.net/{containerName}/{file.FileName}");
    }

    public static IFormFile Image(string fileName = "banner.png", string contentType = "image/png", int bytes = 128)
    {
        var stream = new MemoryStream(new byte[bytes]);
        return new FormFile(stream, 0, bytes, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
