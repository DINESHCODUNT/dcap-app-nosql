using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Security.Extensions
{
    public static class SelectListExtensions
    {
        // Overload 1: getText only
        public static IEnumerable<SelectListItem> ToSelectListItems<T>(
            this IEnumerable<T> source,
            Func<T, string> getText)
        {
            return source.Select(x => new SelectListItem
            {
                Text = getText(x),
                Value = x.ToString()
            });
        }

        // Overload 2: getValue + optional getText
        public static IEnumerable<SelectListItem> ToSelectListItems<T>(
            this IEnumerable<T> source,
            Func<T, string> getValue,
            Func<T, string> getText = null)
        {
            return source.Select(x => new SelectListItem
            {
                Value = getValue(x),
                Text = getText != null ? getText(x) : x.ToString()
            });
        }

    }

}
