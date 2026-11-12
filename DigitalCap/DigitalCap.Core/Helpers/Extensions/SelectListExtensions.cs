using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Helpers.Extensions
{
    public static class SelectListExtensions
    {
        public static IEnumerable<SelectListItem> ToSelectListItems(this Type source)
        {
            if (!source.IsEnum)
                yield break;

            var values = Enum.GetValues(source);

            foreach (var currentValue in values)
            {
                yield return currentValue.ToSelectListItem(
                    getValue: x => (int)currentValue);
            }
        }

        public static IEnumerable<SelectListItem> ToSelectListItems<T>(this IEnumerable<T> source, Func<T, string> getText = null, Func<T, string> getValue = null)
        {
            var result = source?.Select(x => x.ToSelectListItem(getText, getValue))
                            ?? new SelectListItem[0];

            return result;
        }

        public static SelectListItem ToSelectListItem<T>(this T source, Func<T, string> getText = null, Func<T, object> getValue = null)
        {
            var result = new SelectListItem
            {
                Text = getText?.Invoke(source) ?? source.GetDescription(),
                Value = getValue?.Invoke(source)?.ToString() ?? source.ToString()
            };

            return result;
        }
    }
}
