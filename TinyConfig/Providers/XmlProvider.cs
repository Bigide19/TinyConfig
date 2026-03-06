using TinyConfig.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace TinyConfig.Providers
{
    /// <summary>
    /// Reads and writes configuration from an XML file.
    /// Sections map to child elements of the root, keys to elements within them.
    /// </summary>
    public class XmlProvider : ITinyConfig
    {
        private readonly string _filePath;
        private Dictionary<string, Dictionary<string, string>> _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public XmlProvider(string filePath)
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
            var settings = new XmlWriterSettings { Indent = true };
            using (var writer = XmlWriter.Create(_filePath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Config");

                foreach (var section in _data)
                {
                    writer.WriteStartElement(section.Key);
                    foreach (var kvp in section.Value)
                    {
                        writer.WriteElementString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
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

            var doc = new XmlDocument();
            doc.Load(_filePath);

            var root = doc.DocumentElement;
            if (root == null) return;

            foreach (XmlNode sectionNode in root.ChildNodes)
            {
                if (sectionNode.NodeType != XmlNodeType.Element) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlNode keyNode in sectionNode.ChildNodes)
                {
                    if (keyNode.NodeType != XmlNodeType.Element) continue;
                    dict[keyNode.Name] = keyNode.InnerText;
                }
                _data[sectionNode.Name] = dict;
            }
        }
    }
}
