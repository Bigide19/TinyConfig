using System;
using System.ComponentModel;

namespace TinyConfig.Internal
{
    /// <summary>
    /// Converts string values to the requested type using TypeDescriptor.
    /// </summary>
    internal static class ValueConverter
    {
        public static T Convert<T>(string value, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(value);
                }

                return (T)System.Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                // Return default on conversion failure (format mismatch, etc.)
                return defaultValue;
            }
        }
    }
}
