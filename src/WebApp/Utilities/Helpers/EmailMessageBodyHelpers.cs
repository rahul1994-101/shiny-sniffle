using System.Net;
using System.Text.RegularExpressions;

using MimeKit;
using MimeKit.Utils;

namespace WebApp.Utilities.Helpers;

internal static partial class EmailMessageBodyHelpers
{
    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ScriptStyleBlockPattern();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTagPattern();

    [GeneratedRegex(@"</p>", RegexOptions.IgnoreCase)]
    private static partial Regex ParagraphEndPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapsePattern();

    internal sealed record PlainBodyResult(string Text, bool FromHtml);

    internal static PlainBodyResult GetPlainBody(MimeMessage message)
    {
        var text = GetPlainTextOrNull(message);
        if (text is not null)
        {
            return new PlainBodyResult(TruncateBody(text), FromHtml: false);
        }

        var html = message.HtmlBody;
        if (!string.IsNullOrWhiteSpace(html))
        {
            var converted = ConvertHtmlToPlainText(html);
            if (!string.IsNullOrWhiteSpace(converted))
            {
                return new PlainBodyResult(TruncateBody(converted), FromHtml: true);
            }
        }

        return new PlainBodyResult("(no readable body)", FromHtml: false);
    }

    internal static string? GetPlainTextOrNull(MimeMessage message)
    {
        var text = message.TextBody;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    internal static IReadOnlyList<string> GetAttachmentNames(MimeMessage message)
    {
        var names = new List<string>();

        foreach (var entity in message.Attachments)
        {
            if (entity is not MimePart part)
            {
                continue;
            }

            var rawName = part.FileName
                ?? part.ContentDisposition?.FileName
                ?? part.ContentType.Name;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                names.Add("(unnamed)");
                continue;
            }

            names.Add(MimeUtils.Unquote(rawName.Trim()));
        }

        return names;
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        var stripped = ScriptStyleBlockPattern().Replace(html, " ");
        stripped = BreakTagPattern().Replace(stripped, "\n");
        stripped = ParagraphEndPattern().Replace(stripped, "\n");
        stripped = HtmlTagPattern().Replace(stripped, " ");
        stripped = WebUtility.HtmlDecode(stripped);
        return WhitespaceCollapsePattern().Replace(stripped, " ").Trim();
    }

    private static string TruncateBody(string text)
    {
        text = text.Trim();
        return text.Length <= EmailReadConstants.MaxMessageBodyLength
            ? text
            : text[..EmailReadConstants.MaxMessageBodyLength] + "… (truncated)";
    }
}
