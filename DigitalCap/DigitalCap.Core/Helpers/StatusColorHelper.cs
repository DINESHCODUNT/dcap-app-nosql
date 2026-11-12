using System;
using System.Collections.Generic;

namespace DigitalCap.Core.Helpers
{
    public static class StatusColorHelper
    {
        private static readonly Dictionary<string, string> ProjectStatusColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Contracted", "#6c757d" },        // grey
            { "Open", "#17a2b8" },              // light blue (cyan)
            { "Active", "#007bff" },            // darker blue
            { "Ready for Closure", "#28a745" }, // green (handles all case variations due to OrdinalIgnoreCase)
            { "Closed", "#004085" },            // dark blue
            { "Cancelled", "#6c757d" }          // grey
        };

        private const string DefaultColor = "#e9ecef"; // default grey

        /// <summary>
        /// Gets the color for a project status (Contracted, Open, Active, etc.)
        /// </summary>
        public static string GetProjectStatusColor(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return DefaultColor;

            var normalizedStatus = status.Trim();

            if (ProjectStatusColors.TryGetValue(normalizedStatus, out var color))
                return color;

            // Try with normalized key (remove spaces and special characters)
            var normalizedKey = normalizedStatus.Replace(" ", "").Replace("-", "").Replace("_", "");
            foreach (var kvp in ProjectStatusColors)
            {
                var normalizedDictKey = kvp.Key.Replace(" ", "").Replace("-", "").Replace("_", "");
                if (string.Equals(normalizedKey, normalizedDictKey, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return DefaultColor;
        }

        /// <summary>
        /// Adds a new project status color mapping or updates an existing one
        /// </summary>
        public static void SetProjectStatusColor(string status, string color)
        {
            if (string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(color))
                return;

            ProjectStatusColors[status.Trim()] = color.Trim();
        }

        /// <summary>
        /// Gets all project status color mappings
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetAllProjectStatusColors()
        {
            return ProjectStatusColors;
        }
    }
}
