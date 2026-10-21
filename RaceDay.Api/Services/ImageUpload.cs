namespace RaceDay.Api.Services;

public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };

    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0) return "The uploaded image is empty.";
        if (file.Length > MaxBytes) return "Images must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG, WEBP or GIF images can be uploaded.";
        return null;
    }
}
