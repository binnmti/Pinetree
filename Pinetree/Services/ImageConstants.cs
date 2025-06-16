namespace Pinetree.Services;

public static class ImageConstants
{
    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml"
    };

    public const int MaxFileSizeMB = 5;


    public static string[] AllowedExtensions => [.. ContentTypes.Select(x => x.Key)];

    public static int MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;

    public static bool IsAllowedExtension(string extension) 
        => AllowedExtensions.Contains(extension.ToLowerInvariant());

    public static string GetContentType(string extension) 
        => ContentTypes.TryGetValue(extension.ToLowerInvariant(), out var contentType)
            ? contentType
            : "application/octet-stream";
}
