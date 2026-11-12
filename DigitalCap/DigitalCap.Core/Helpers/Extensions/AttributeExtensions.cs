using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace DigitalCap.Core.Helpers.Extensions
{
    public static class AttributeExtensions
    {
        public static string GetDescription<T>(this T source)
        {
            var type = source.GetType();
            var memberValue = type.GetMember(source.ToString())
                                  .FirstOrDefault();
            var descriptionAttribute = memberValue?.GetCustomAttribute<DescriptionAttribute>();
            var description = descriptionAttribute?.Description ?? source.ToString();

            return description;
        }
    }
}
