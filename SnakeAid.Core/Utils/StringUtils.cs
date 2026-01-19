using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SnakeAid.Core.Utils
{
    public static class SlugGenerator
    {
        public static string DefaultSlug(params string[] input)
        {
            var combined = string.Join(" ", input.Where(s => !string.IsNullOrWhiteSpace(s)));

            string normalized = combined.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            string noAccent = sb.ToString().Normalize(NormalizationForm.FormC);

            string lower = noAccent.ToLowerInvariant();

            string slug = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            slug = Regex.Replace(slug, "-{2,}", "-");

            return slug;
        }

        public static string WithGuid(string baseSlug)
        {
            return $"{baseSlug}-{Guid.NewGuid().ToString("N")[..8]}";
        }
    }

    public static class StringNormalizer
    {
        public static string NormalizeForComparison(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 1. Trim & collapse whitespace
            var trimmed = input.Trim();

            // Replace non-breaking space with normal space
            trimmed = trimmed.Replace('\u00A0', ' ');

            // Collapse multiple spaces into one
            trimmed = Regex.Replace(trimmed, @"\s+", " ");

            // 2. Normalize Unicode (avoid weird composed chars)
            trimmed = trimmed.Normalize(NormalizationForm.FormC);

            // 3. Lowercase for case-insensitive compare
            return trimmed.ToLowerInvariant();
        }
    }

}
