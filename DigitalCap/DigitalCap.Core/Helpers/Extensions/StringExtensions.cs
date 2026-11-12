using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalCap.Core.Helpers.Extensions
{
    public static class StringExtensions
    {
        public static string JoinNonEmpty<T>(this IEnumerable<T> source, string separator)
        {
            if (source == null)
                return null;

            var stringValues = source.Select(x => x?.ToString());
            var validValues = stringValues.Where(x => !string.IsNullOrWhiteSpace(x));
            var distinctValues = validValues.Distinct();
            var joinedValues = string.Join(separator, distinctValues);

            return joinedValues;
        }

        public static string CamelCaseToWords(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;

            var result = Regex.Replace(
                input: source,
                pattern: @"(?<=[A-Z])(?=[A-Z][a-z]) |
                  (?<=[^A-Z])(?=[A-Z]) |
                  (?<=[A-Za-z])(?=[^A-Za-z])",
                replacement: " ",
                options: RegexOptions.IgnorePatternWhitespace);

            return result;
        }

        public static string ToValidFileName(this string source)
        {
            var pattern = string.Join("|", System.IO.Path.GetInvalidFileNameChars().Select(x => $"\\{x}"));
            var result = Regex.Replace(source, pattern, "_");

            return result;
        }
    }
}
