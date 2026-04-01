using System.Net;
using System.Text.RegularExpressions;
using TaskAutomation.Application.Services;

namespace TaskAutomation.Infrastructure.Services;

public sealed partial class HtmlTextSanitizer : IHtmlTextSanitizer
{
    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var normalized = WhitespaceRegex().Replace(decoded, " ").Trim();

        return normalized;
    }

    [GeneratedRegex("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
