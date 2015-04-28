using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{
    public static class StringExtensions
    {
        public static T ParseEnum<T>(this string enumValue)
    where T : struct, IConvertible
        {
            return EnumUtil<T>.Parse(enumValue);
        }
        /// <summary>
        /// Returnerar nullDescription om sträng har nullvärde eller är tom, annars returneras strängens värde
        /// </summary>
        public static string GetValueOrDefault(this string stringValue, string nullDescription)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                return nullDescription;
            else
                return stringValue;
        }
    }
}
