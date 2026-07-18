using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TMDT1_TH.Infrastructure;

public static partial class SlugHelper
{
    public static string Generate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim()
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var text = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        text = InvalidSlugCharacterRegex().Replace(text, "-");
        text = RepeatedDashRegex().Replace(text, "-");
        return text.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex InvalidSlugCharacterRegex();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex RepeatedDashRegex();
}
