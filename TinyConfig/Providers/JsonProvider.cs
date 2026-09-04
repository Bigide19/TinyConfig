using TinyConfig.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TinyConfig.Providers
{
    /// <summary>
    /// Reads and writes configuration from a JSON file.
    /// Sections map to top-level JSON objects, keys to properties within them.
    /// </summary>
    public class JsonProvider : ITinyConfig
    {
        private readonly string _filePath;
        private Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public JsonProvider(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        public string Get(string section, string key, string defaultValue = "", bool autoSave = false)
        {
            if (_data.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var val))
                return val;

            if (autoSave) Set(section, key, defaultValue);
            return defaultValue;
        }

        public T Get<T>(string section, string key, T defaultValue = default(T), bool autoSave = false)
        {
            string valStr = Get(section, key, null, false);

            if (string.IsNullOrWhiteSpace(valStr))
            {
                if (autoSave) Set(section, key, defaultValue);
                return defaultValue;
            }

            return ValueConverter.Convert(valStr, defaultValue);
        }

        public bool TryGet<T>(string section, string key, out T value)
        {
            return ValueConverter.TryConvert(Get(section, key, null, false), out value);
        }

        public void Set<T>(string section, string key, T value)
        {
            if (!_data.ContainsKey(section))
                _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _data[section][key] = value?.ToString() ?? "";
            Save();
        }

        public bool Exists(string section, string key)
        {
            return _data.TryGetValue(section, out var sectionDict) && sectionDict.ContainsKey(key);
        }

        private void Save()
        {
            EnsureDirectory();
            var options = new JsonWriterOptions { Indented = true };
            using (var stream = File.Create(_filePath))
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                foreach (var section in _data)
                {
                    writer.WriteStartObject(section.Key);
                    foreach (var kvp in section.Value)
                    {
                        writer.WriteString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
        }

        private void EnsureDirectory()
        {
            string dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;

            var bytes = File.ReadAllBytes(_filePath);
            var doc = JsonDocument.Parse(bytes);

            foreach (var section in doc.RootElement.EnumerateObject())
            {
                if (section.Value.ValueKind != JsonValueKind.Object) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in section.Value.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ToString();
                }
                _data[section.Name] = dict;
            }

            doc.Dispose();
        }
    }
}
