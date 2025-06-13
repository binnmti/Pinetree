using System.Text.RegularExpressions;

namespace Pinetree.Services;

public class VersionService(IWebHostEnvironment environment)
{
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<string> GetLatestVersionAsync()
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, "Changelog.md");
            if (!File.Exists(filePath))
            {
                return "1.0.0";
            }
            
            var content = await File.ReadAllTextAsync(filePath);
            var version = ExtractLatestVersion(content);
            return version;
        }
        catch (Exception)
        {
            return "1.0.0"; // Fallback version
        }
    }

    private static string ExtractLatestVersion(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Match version headers like "## [0.9.2] - 2025-06-12"
            var versionMatch = Regex.Match(trimmedLine, @"^## \[(.+?)\] - (.+)$");
            if (versionMatch.Success)
            {
                return versionMatch.Groups[1].Value;
            }
        }
        
        return "1.0.0"; // Fallback version
    }
}
