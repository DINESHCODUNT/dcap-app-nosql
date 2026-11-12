using DigitalCap.Core.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalCap.Core.Helpers.Constants
{
    public static class Template
    {
        public static readonly string TemplateDataRootName = "DigitalCAPFields";

        public static readonly string TokenPrefix = "##";
        public static readonly string TokenSuffix = "##";
        public static readonly Func<string, string> Tokenize = x => $"{TokenPrefix}{x}{TokenSuffix}";

        public static readonly string ManualEntryPrefix = "«";
        public static readonly string ManualEntrySuffix = "»";

        public static string TokenizeManualEntry(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source;

            var cleanedSource = Regex.Replace(source, $"{TokenPrefix}|{TokenSuffix}", string.Empty);
            var sourceWords = cleanedSource.CamelCaseToWords();
            var tokenizedSource = $"{ManualEntryPrefix}Enter {sourceWords}{ManualEntrySuffix}";

            return tokenizedSource;
        }
    }
}
