namespace TinyConfig
{
    /// <summary>
    /// Unified interface for reading and writing configuration values.
    /// </summary>
    public interface ITinyConfig
    {
        /// <summary>Gets a string value. Returns <paramref name="defaultValue"/> if not found.</summary>
        /// <param name="section">Section (INI section, JSON object, XML element, Registry sub-key).</param>
        /// <param name="key">Key name.</param>
        /// <param name="defaultValue">Value to return when the key does not exist.</param>
        /// <param name="autoSave">If true, writes the default value when the key is missing.</param>
        string Get(string section, string key, string defaultValue = "", bool autoSave = false);

        /// <summary>Gets a typed value, converted via TypeConverter.</summary>
        T Get<T>(string section, string key, T defaultValue = default(T), bool autoSave = false);

        /// <summary>Sets a value. The value is stored as a string.</summary>
        void Set<T>(string section, string key, T value);

        /// <summary>Returns true if the specified section and key exist.</summary>
        bool Exists(string section, string key);
    }
}
