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
        Sanitizer.AllowedClasses.Add("math-display");        // Basic HTML tags commonly supported in Markdown editors
        Sanitizer.AllowedTags.Add("strong");
        Sanitizer.AllowedTags.Add("b");
        Sanitizer.AllowedTags.Add("em");
        Sanitizer.AllowedTags.Add("i");
        Sanitizer.AllowedTags.Add("u");
        Sanitizer.AllowedTags.Add("s");
        Sanitizer.AllowedTags.Add("del");
        Sanitizer.AllowedTags.Add("sup");
        Sanitizer.AllowedTags.Add("sub");
        Sanitizer.AllowedTags.Add("mark");
        Sanitizer.AllowedTags.Add("br");
        Sanitizer.AllowedTags.Add("hr");
        
        // Structure tags
        Sanitizer.AllowedTags.Add("div");
        Sanitizer.AllowedTags.Add("span");
        Sanitizer.AllowedTags.Add("p");
        Sanitizer.AllowedTags.Add("blockquote");
        Sanitizer.AllowedTags.Add("details");
        Sanitizer.AllowedTags.Add("summary");
        
        // Table tags (already supported by Markdig, but ensuring HTML tables work)
        Sanitizer.AllowedTags.Add("table");
        Sanitizer.AllowedTags.Add("thead");
        Sanitizer.AllowedTags.Add("tbody");
        Sanitizer.AllowedTags.Add("tr");
        Sanitizer.AllowedTags.Add("th");
        Sanitizer.AllowedTags.Add("td");
        Sanitizer.AllowedTags.Add("colgroup");
        Sanitizer.AllowedTags.Add("col");

        // Image tag with size attributes
        Sanitizer.AllowedTags.Add("img");
        Sanitizer.AllowedAttributes.Add("alt");
        Sanitizer.AllowedAttributes.Add("title");        // YouTube iframe settings
        Sanitizer.AllowedAttributes.Add("src");        // YouTube iframe settings
        Sanitizer.AllowedTags.Add("iframe");
        Sanitizer.AllowedAttributes.Add("src");
        Sanitizer.AllowedAttributes.Add("width");
        Sanitizer.AllowedAttributes.Add("height");
        Sanitizer.AllowedAttributes.Add("frameborder");
        Sanitizer.AllowedAttributes.Add("allow");
        Sanitizer.AllowedAttributes.Add("allowfullscreen");
        Sanitizer.AllowedAttributes.Add("sandbox");
        Sanitizer.AllowedAttributes.Add("loading");
        Sanitizer.AllowedAttributes.Add("class");
        Sanitizer.AllowedAttributes.Add("id");
        
        // Common HTML attributes
        Sanitizer.AllowedAttributes.Add("style");
        
        // CSS properties (limited set for safety)
        Sanitizer.AllowedCssProperties.Add("color");
        Sanitizer.AllowedCssProperties.Add("background-color");
        Sanitizer.AllowedCssProperties.Add("font-size");
        Sanitizer.AllowedCssProperties.Add("font-weight");
        Sanitizer.AllowedCssProperties.Add("text-align");
        Sanitizer.AllowedCssProperties.Add("margin");
        Sanitizer.AllowedCssProperties.Add("padding");
        Sanitizer.AllowedCssProperties.Add("border");
        Sanitizer.AllowedCssProperties.Add("width");
        Sanitizer.AllowedCssProperties.Add("height");
        Sanitizer.AllowedCssProperties.Add("max-width");
        Sanitizer.AllowedCssProperties.Add("max-height");
        Sanitizer.AllowedCssProperties.Add("display");
        Sanitizer.AllowedCssProperties.Add("float");
        Sanitizer.AllowedCssProperties.Add("text-decoration");
        
        // Allowed CSS classes
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