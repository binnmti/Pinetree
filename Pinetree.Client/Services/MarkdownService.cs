using Ganss.Xss;
using Markdig;

namespace Pinetree.Client.Services;

public static class MarkdownService
{
    private static MarkdownPipeline MarkdownPipeline { get; } = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAdvancedExtensions()
            .Build();

    public static string ToHtml(string markdown)
    {
        var html = Markdown.ToHtml(markdown, MarkdownPipeline);
        return new HtmlSanitizer().Sanitize(html);
    }
}