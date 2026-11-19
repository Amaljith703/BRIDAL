using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MakeupArt.Application.Common;

public static partial class Slugger
{
    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonSlug();

    public static string Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Guid.NewGuid().ToString("N")[..8];
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(ch));
        }
        var slug = NonSlug().Replace(builder.ToString(), "-").Trim('-');
        return slug.Length > 90 ? slug[..90].Trim('-') : slug;
    }

    public static string Unique(string value, IEnumerable<string> existing)
    {
        var baseSlug = Create(value);
        var set = existing.Where(s => !string.IsNullOrWhiteSpace(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(baseSlug)) return baseSlug;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!set.Contains(candidate)) return candidate;
        }
        return $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(100, baseSlug.Length + 9)];
    }
}
