using System;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public static class SqlDate
    {
        /// <summary>
        /// SQL DATEDIFF(DAY, a, b) counts midnights crossed: (b.Date - a.Date).Days.
        /// Returns null if either input is null.
        /// </summary>
        public static int? DiffDays(DateTime? a, DateTime? b)
        {
            if (!a.HasValue || !b.HasValue)
            {
                return null;
            }

            return (b.Value.Date - a.Value.Date).Days;
        }

        public static int DiffDays(DateTime a, DateTime b)
        {
            return (b.Date - a.Date).Days;
        }
    }
}
