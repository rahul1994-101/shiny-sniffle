using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace WebApp.Formatting;

/// <summary>
/// Renders assistant/system chat text as Markdown, then sanitizes HTML for safe <see cref="MarkupString"/> use.
/// </summary>
public static class ChatMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAutoLinks()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    private static HtmlSanitizer CreateSanitizer()
    {
        var s = new HtmlSanitizer();
        foreach (var tag in new[]
                 {
                     "a", "h1", "h2", "h3", "h4", "h5", "h6", "p", "br", "hr", "div", "span",
                     "ul", "ol", "li", "blockquote", "pre", "code", "strong", "em", "del", "sub", "sup",
                     "table", "thead", "tbody", "tfoot", "tr", "th", "td",
                 })
        {
            s.AllowedTags.Add(tag);
        }

        s.AllowedAttributes.Add("class");
        s.AllowedAttributes.Add("colspan");
        s.AllowedAttributes.Add("rowspan");
        s.AllowedSchemes.Add("mailto");
        return s;
    }

    public static MarkupString ToSanitizedHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new MarkupString("");
        }

        var rawHtml = Markdown.ToHtml(markdown.Trim(), Pipeline);
        var safe = Sanitizer.Sanitize(rawHtml);
        return new MarkupString(safe);
    }
}
