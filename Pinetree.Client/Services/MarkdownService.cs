using Ganss.Xss;
using Markdig;

namespace Pinetree.Client.Services;

public static class MarkdownService
{
    private static MarkdownPipeline MarkdownPipeline { get; } = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAdvancedExtensions()
            .UseMathematics() // 数式処理の拡張を追加
            .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    static MarkdownService()
    {
        Sanitizer.AllowedSchemes.Add("blob");

        // 数式のタグと属性を許可する
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

        // 数式用クラスを許可する
        Sanitizer.AllowedClasses.Add("math");
        Sanitizer.AllowedClasses.Add("math-inline");
        Sanitizer.AllowedClasses.Add("math-display");
    }
    public static string ToHtml(string markdown)
    {
        var html = Markdown.ToHtml(markdown, MarkdownPipeline);
        var sanitizedHtml = Sanitizer.Sanitize(html);
        return sanitizedHtml.Replace(" disabled=\"disabled\"", "")
                           .Replace(" disabled", "");
    }
}