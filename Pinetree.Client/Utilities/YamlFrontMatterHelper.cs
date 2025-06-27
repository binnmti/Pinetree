namespace Pinetree.Client.Utilities;

/// <summary>
/// Helper class for processing YAML Front Matter in markdown content
/// </summary>
public static class YamlFrontMatterHelper
{
    /// <summary>
    /// Removes YAML Front Matter from markdown content
    /// </summary>
    /// <param name="content">The markdown content with potential YAML Front Matter</param>
    /// <returns>Content with YAML Front Matter removed</returns>
    public static string RemoveYamlFrontMatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        // Check if content starts with YAML Front Matter (---)
        if (content.TrimStart().StartsWith("---"))
        {
            var lines = content.Split('\n');
            var (frontMatterStart, frontMatterEnd) = FindYamlFrontMatterBoundaries(lines);

            // If we found both start and end markers, remove the YAML Front Matter
            if (frontMatterStart != -1 && frontMatterEnd != -1)
            {
                var remainingLines = lines.Skip(frontMatterEnd + 1);
                return string.Join('\n', remainingLines).TrimStart('\n');
            }
        }

        return content;
    }

    /// <summary>
    /// Extracts a specific value from YAML Front Matter
    /// </summary>
    /// <param name="content">The markdown content with potential YAML Front Matter</param>
    /// <param name="key">The YAML key to extract</param>
    /// <returns>The value associated with the key, or empty string if not found</returns>
    public static string GetYamlFrontMatterValue(string? content, string key)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Check if content starts with YAML Front Matter (---)
        if (!content.TrimStart().StartsWith("---"))
            return string.Empty;

        var lines = content.Split('\n');
        var (frontMatterStart, frontMatterEnd) = FindYamlFrontMatterBoundaries(lines);

        // If we found both start and end markers, parse the YAML content
        if (frontMatterStart != -1 && frontMatterEnd != -1)
        {
            for (int i = frontMatterStart + 1; i < frontMatterEnd; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith($"{key}:"))
                {
                    var value = line.Substring($"{key}:".Length).Trim();
                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    return value;
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Checks if the content contains YAML Front Matter
    /// </summary>
    /// <param name="content">The markdown content to check</param>
    /// <returns>True if YAML Front Matter is present, false otherwise</returns>
    public static bool HasYamlFrontMatter(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("---");
    }

    /// <summary>
    /// Extracts all YAML Front Matter as key-value pairs
    /// </summary>
    /// <param name="content">The markdown content with potential YAML Front Matter</param>
    /// <returns>Dictionary of YAML key-value pairs</returns>
    public static Dictionary<string, string> GetYamlFrontMatterData(string? content)
    {
        var result = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(content))
            return result;

        // Check if content starts with YAML Front Matter (---)
        if (!content.TrimStart().StartsWith("---"))
            return result;

        var lines = content.Split('\n');
        var (frontMatterStart, frontMatterEnd) = FindYamlFrontMatterBoundaries(lines);

        // If we found both start and end markers, parse all YAML content
        if (frontMatterStart != -1 && frontMatterEnd != -1)
        {
            for (int i = frontMatterStart + 1; i < frontMatterEnd; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains(':'))
                {
                    var colonIndex = line.IndexOf(':');
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    
                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the start and end boundaries of YAML Front Matter in content lines
    /// </summary>
    /// <param name="lines">Array of content lines</param>
    /// <returns>Tuple containing start and end line indices, or (-1, -1) if not found</returns>
    private static (int start, int end) FindYamlFrontMatterBoundaries(string[] lines)
    {
        var frontMatterStart = -1;
        var frontMatterEnd = -1;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim();
            if (trimmedLine == "---")
            {
                if (frontMatterStart == -1)
                {
                    frontMatterStart = i;
                }
                else if (frontMatterEnd == -1)
                {
                    frontMatterEnd = i;
                    break;
                }
            }
        }
        
        return (frontMatterStart, frontMatterEnd);
    }
}
