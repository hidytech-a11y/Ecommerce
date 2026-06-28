using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ecommerce.Application.Common.Utilities;

public static class SlugGenerator
{
    // Converts any text into a URL-friendly slug.
    public static string Generate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.ToLowerInvariant();

        text = RemoveAccents(text);

        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        text = Regex.Replace(text, @"[\s-]+", "-");

        text = text.Trim('-');

        if (text.Length > 200)
            text = text.Substring(0, 200).TrimEnd('-');

        return text;
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}