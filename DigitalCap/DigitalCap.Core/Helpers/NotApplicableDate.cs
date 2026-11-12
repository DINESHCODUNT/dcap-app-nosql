using System;

namespace DigitalCap.Core.Helpers
{
    // Some report dates are stored in non-nullable date columns, so a date entered as "N/A"
    // is saved as this reserved date to tell it apart from a date that was left blank.
    // Keep in sync with digital-cap/Helpers/NotApplicableDate.cs.
    public static class NotApplicableDate
    {
        public const string Text = "N/A";
        public static readonly DateTime Value = new DateTime(1900, 1, 1);

        public static bool IsText(string value) =>
            string.Equals(value?.Replace(" ", ""), Text, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value?.Replace(" ", ""), "NA", StringComparison.OrdinalIgnoreCase);

        public static string ToStorable(string value) => IsText(value) ? Value.ToString("yyyy-MM-dd") : value;

        // H&I dates use 01-Jan-1901 and earlier for "blank", so only later dates or the N/A date are shown
        public static bool HasValue(DateTime value) => value > new DateTime(1901, 1, 1) || value.Date == Value;
    }
}
