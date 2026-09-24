using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace web.Infrastructure
{
    /// <summary>
    /// Renders a plain-text email body (the same text used for SMS) as HTML: bare http(s) links
    /// become clickable &lt;a href&gt; tags and line breaks become &lt;br&gt;. Sent as the HTML
    /// alternative alongside the plain-text body — SMS always stays plain text.
    /// </summary>
    public static class EmailHtmlBuilder
    {
        private static readonly Regex UrlPattern = new(@"https?://[^\s<>""]+", RegexOptions.Compiled);

        public static string FromPlainText(string plainBody)
        {
            var html = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in UrlPattern.Matches(plainBody))
            {
                html.Append(WebUtility.HtmlEncode(plainBody[lastIndex..match.Index]));
                var encodedUrl = WebUtility.HtmlEncode(match.Value);
                html.Append($"<a href=\"{encodedUrl}\">{encodedUrl}</a>");
                lastIndex = match.Index + match.Length;
            }

            html.Append(WebUtility.HtmlEncode(plainBody[lastIndex..]));

            var htmlBody = html.ToString().Replace("\r\n", "\n").Replace("\n", "<br>\n");
            return $"<!DOCTYPE html><html><body style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1a1a1a;\">{htmlBody}</body></html>";
        }
    }
}
