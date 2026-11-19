using Ganss.Xss;

namespace MakeupArt.Infrastructure.Services;

public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}

/// <summary>
/// Cleans admin-authored HTML before it is stored or rendered on the storefront.
/// A fresh sanitizer instance is used per call because HtmlSanitizer is not guaranteed thread-safe.
/// </summary>
public class HtmlSanitizerService : IHtmlSanitizerService
{
    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "em", "b", "i", "u", "s", "ul", "ol", "li", "blockquote", "h2", "h3", "h4",
                     "a", "span", "div", "figure", "figcaption", "img", "table", "thead", "tbody", "tr", "th", "td", "hr"
                 })
            sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title", "target", "rel", "src", "alt", "width", "height", "class", "style" })
            sanitizer.AllowedAttributes.Add(attribute);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");
        sanitizer.AllowedCssProperties.Clear();
        foreach (var property in new[] { "color", "background-color", "font-weight", "font-style", "text-align", "text-decoration" })
            sanitizer.AllowedCssProperties.Add(property);
        sanitizer.AllowDataAttributes = false;
        return sanitizer.Sanitize(html.Trim());
    }
}
