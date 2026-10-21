namespace RaceDay.Api.Services;

/// <summary>Checks an uploaded image before it is sent to Blob Storage.</summary>
public static class ImageUpload
{
    /// <summary>Largest image accepted: 5 MB.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0) return "The uploaded image is empty.";
        if (file.Length > MaxBytes) return "Images must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG, WEBP or GIF images can be uploaded.";
        return null;
    }
}
