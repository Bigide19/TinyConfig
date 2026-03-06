using TinyConfig.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace TinyConfig.Providers
{
    /// <summary>
    /// Reads and writes configuration from an INI file.
    /// Sections map to [Section] headers, keys to key=value pairs.
    /// </summary>
    public class IniProvider : ITinyConfig
    {
        private readonly string _filePath;
        private Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public IniProvider(string filePath)
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
            if (valStr == null)
            {
                if (autoSave) Set(section, key, defaultValue);
                return defaultValue;
            }
            return ValueConverter.Convert(valStr, defaultValue);
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
            List<string> lines = new List<string>();
            foreach (var section in _data)
            {
                lines.Add("[" + section.Key + "]");
                foreach (var kvp in section.Value)
                    lines.Add(kvp.Key + "=" + kvp.Value);
                lines.Add("");
            }
            File.WriteAllLines(_filePath, lines.ToArray());
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

            string current = "Default";
            string[] allLines = File.ReadAllLines(_filePath);

            foreach (string line in allLines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";")) continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    current = trimmed.Substring(1, trimmed.Length - 2);
                }
                else
                {
                    int index = trimmed.IndexOf('=');
                    if (index > 0)
                    {
                        string k = trimmed.Substring(0, index).Trim();
                        string v = trimmed.Substring(index + 1).Trim();

                        if (!_data.ContainsKey(current))
                            _data[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        _data[current][k] = v;
                    }
                }
            }
        }
    }
}
