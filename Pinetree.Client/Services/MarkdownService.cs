using Ganss.Xss;
using Markdig;

namespace Pinetree.Client.Services;

public static class MarkdownService
{
    private static MarkdownPipeline MarkdownPipeline { get; } = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAdvancedExtensions()
            .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    static MarkdownService()
    {
        Sanitizer.AllowedSchemes.Add("blob");
    }

    public static string ToHtml(string markdown)
    {
        var html = Markdown.ToHtml(markdown, MarkdownPipeline);
        return Sanitizer.Sanitize(html);
    }
}