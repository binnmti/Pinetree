using Ganss.Xss;
using Markdig;

namespace Pinetree.Client.Services;

public static class MarkdownService
{
    private static MarkdownPipeline MarkdownPipeline { get; } = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseMathematics()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    static MarkdownService()
    {
        Sanitizer.AllowedSchemes.Add("blob");
        Sanitizer.AllowedSchemes.Add("https");

        // Math rendering tags and attributes
        Sanitizer.AllowedTags.Add("math");
        Sanitizer.AllowedTags.Add("annotation");
        Sanitizer.AllowedTags.Add("semantics");
        Sanitizer.AllowedTags.Add("mrow");
        Sanitizer.AllowedTags.Add("mi");
        Sanitizer.AllowedTags.Add("mo");
        Sanitizer.AllowedTags.Add("mn");
        Sanitizer.AllowedTags.Add("msup");
        Sanitizer.AllowedTags.Add("msub");
        Sanitizer.AllowedTags.Add("mfrac");

        Sanitizer.AllowedClasses.Add("math");
        Sanitizer.AllowedClasses.Add("math-inline");
        Sanitizer.AllowedClasses.Add("math-display");

        // YouTube iframe settings
        Sanitizer.AllowedTags.Add("iframe");
        Sanitizer.AllowedTags.Add("div");
        Sanitizer.AllowedAttributes.Add("src");
        Sanitizer.AllowedAttributes.Add("width");
        Sanitizer.AllowedAttributes.Add("height");
        Sanitizer.AllowedAttributes.Add("frameborder");
        Sanitizer.AllowedAttributes.Add("allow");
        Sanitizer.AllowedAttributes.Add("allowfullscreen");
        Sanitizer.AllowedAttributes.Add("sandbox");
        Sanitizer.AllowedAttributes.Add("loading");
        Sanitizer.AllowedAttributes.Add("class");
        Sanitizer.AllowedClasses.Add("youtube-container");
    }

    public static string ToHtml(string markdown)
    {
        var processedMarkdown = ProcessYouTubeCustomSyntax(markdown);
        var html = Markdown.ToHtml(processedMarkdown, MarkdownPipeline);
        var sanitizedHtml = Sanitizer.Sanitize(html);
        
        return sanitizedHtml.Replace(" disabled=\"disabled\"", "")
                           .Replace(" disabled", "");
    }

    private static string ProcessYouTubeCustomSyntax(string markdown)
    {
        var youtubePattern = @"\[youtube:([a-zA-Z0-9_-]{11})\]";
        
        return System.Text.RegularExpressions.Regex.Replace(markdown, youtubePattern, match =>
        {
            var videoId = match.Groups[1].Value;
            
            // Skip placeholder "youtubeID"
            if (videoId == "youtubeID")
            {
                return match.Value;
            }
            
            return $@"<div class=""youtube-container"">
                        <iframe src=""https://www.youtube.com/embed/{videoId}"" 
                                frameborder=""0"" 
                                sandbox=""allow-scripts allow-same-origin allow-presentation"" 
                                allow=""accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"" 
                                allowfullscreen 
                                loading=""lazy"">
                        </iframe>
                      </div>";
        });
    }
}