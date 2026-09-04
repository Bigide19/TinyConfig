using System;
using System.ComponentModel;

namespace TinyConfig.Internal
{
    /// <summary>
    /// Converts string values to the requested type using TypeDescriptor.
    /// </summary>
    internal static class ValueConverter
    {
        /// <summary>Converts <paramref name="value"/>, falling back to <paramref name="defaultValue"/>.</summary>
        public static T Convert<T>(string value, T defaultValue)
        {
            T result;
            return TryConvert(value, out result) ? result : defaultValue;
        }

        /// <summary>Converts <paramref name="value"/>. Returns false when it is empty or does not match the type.</summary>
        public static bool TryConvert<T>(string value, out T result)
        {
            result = default(T);
            if (string.IsNullOrWhiteSpace(value)) return false;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    result = (T)converter.ConvertFromString(value);
                    return true;
                }

                result = (T)System.Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }
    }
}
