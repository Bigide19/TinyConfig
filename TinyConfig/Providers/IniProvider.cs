using TinyConfig.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TinyConfig.Providers
{
    /// <summary>
    /// Reads and writes configuration from an INI file.
    /// Sections map to [Section] headers, keys to key=value pairs.
    /// Comments, blank lines, key order and spacing are preserved on write.
    /// </summary>
    public class IniProvider : ITinyConfig
    {
        /// <summary>Section for keys that appear before any [Section] header.</summary>
        private const string DefaultSection = "Default";

        private readonly string _filePath;
        private readonly Encoding _encoding;
        private readonly List<string> _lines = new List<string>();

        /// <summary>Section to key to line index in <see cref="_lines"/>.</summary>
        private Dictionary<string, Dictionary<string, int>> _index =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Creates a provider over the given INI file, read as UTF-8 without BOM.</summary>
        public IniProvider(string filePath) : this(filePath, null)
        {
        }

        /// <summary>Creates a provider over the given INI file with an explicit encoding.</summary>
        /// <param name="filePath">Path to the INI file. It does not need to exist yet.</param>
        /// <param name="encoding">Encoding for reading and writing. Defaults to UTF-8 without BOM.</param>
        public IniProvider(string filePath, Encoding encoding)
        {
            _filePath = filePath;
            _encoding = encoding ?? new UTF8Encoding(false);
            Load();
        }

        /// <inheritdoc/>
        public string Get(string section, string key, string defaultValue = "", bool autoSave = false)
        {
            int lineNo;
            if (TryFindLine(section, key, out lineNo))
                return ParseValue(_lines[lineNo]);

            if (autoSave) Set(section, key, defaultValue);
            return defaultValue;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool TryGet<T>(string section, string key, out T value)
        {
            value = default(T);

            int lineNo;
            if (!TryFindLine(section, key, out lineNo)) return false;

            return ValueConverter.TryConvert(ParseValue(_lines[lineNo]), out value);
        }

        /// <inheritdoc/>
        public void Set<T>(string section, string key, T value)
        {
            string text = value == null ? "" : value.ToString();

            int lineNo;
            if (TryFindLine(section, key, out lineNo))
            {
                _lines[lineNo] = ReplaceValue(_lines[lineNo], text);
            }
            else
            {
                InsertEntry(section, key, text);
                Reindex();
            }

            Save();
        }

        /// <inheritdoc/>
        public bool Exists(string section, string key)
        {
            int lineNo;
            return TryFindLine(section, key, out lineNo);
        }

        #region Load / Save

        private void Load()
        {
            _lines.Clear();

            if (File.Exists(_filePath))
                _lines.AddRange(File.ReadAllLines(_filePath, _encoding));

            Reindex();
        }

        private void Save()
        {
            EnsureDirectory();
            File.WriteAllLines(_filePath, _lines.ToArray(), _encoding);
        }

        private void EnsureDirectory()
        {
            string dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>Rebuilds the section/key to line map. The last duplicate key wins.</summary>
        private void Reindex()
        {
            _index = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            string current = DefaultSection;

            for (int i = 0; i < _lines.Count; i++)
            {
                string trimmed = _lines[i].Trim();
                if (trimmed.Length == 0 || IsComment(trimmed)) continue;

                if (IsSectionHeader(trimmed))
                {
                    current = SectionNameOf(trimmed);
                    EnsureSection(current);
                    continue;
                }

                int sep = trimmed.IndexOf('=');
                if (sep <= 0) continue;

                EnsureSection(current);
                _index[current][trimmed.Substring(0, sep).Trim()] = i;
            }
        }

        private void EnsureSection(string section)
        {
            if (!_index.ContainsKey(section))
                _index[section] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Line helpers

        private bool TryFindLine(string section, string key, out int lineNo)
        {
            lineNo = -1;
            if (section == null || key == null) return false;

            Dictionary<string, int> keys;
            return _index.TryGetValue(section, out keys) && keys.TryGetValue(key, out lineNo);
        }

        private static bool IsComment(string trimmed)
        {
            return trimmed.StartsWith(";") || trimmed.StartsWith("#");
        }

        private static bool IsSectionHeader(string trimmed)
        {
            return trimmed.Length >= 2 && trimmed.StartsWith("[") && trimmed.EndsWith("]");
        }

        private static string SectionNameOf(string trimmedHeader)
        {
            return trimmedHeader.Substring(1, trimmedHeader.Length - 2).Trim();
        }

        /// <summary>Reads the value part of a key=value line.</summary>
        private static string ParseValue(string line)
        {
            int sep = line.IndexOf('=');
            return sep < 0 ? "" : line.Substring(sep + 1).Trim();
        }

        /// <summary>Replaces the value part, keeping the key and the spacing around the separator.</summary>
        private static string ReplaceValue(string line, string value)
        {
            int sep = line.IndexOf('=');
            if (sep < 0) return line;

            string head = line.Substring(0, sep + 1);
            string tail = line.Substring(sep + 1);
            int pad = tail.Length - tail.TrimStart().Length;

            return head + tail.Substring(0, pad) + value;
        }

        #endregion

        #region Insert

        /// <summary>Adds a key inside its section, or appends a new section at the end.</summary>
        private void InsertEntry(string section, string key, string value)
        {
            string entry = key + "=" + value;
            int header = FindSectionHeaderLine(section);

            if (header >= 0)
            {
                _lines.Insert(LastEntryLineOf(header + 1), entry);
                return;
            }

            if (string.Equals(section, DefaultSection, StringComparison.OrdinalIgnoreCase))
            {
                _lines.Insert(LastEntryLineOf(0), entry);
                return;
            }

            if (_lines.Count > 0 && _lines[_lines.Count - 1].Trim().Length > 0)
                _lines.Add("");

            _lines.Add("[" + section + "]");
            _lines.Add(entry);
        }

        /// <summary>Returns the line index after the last entry of a section, skipping trailing comments.</summary>
        private int LastEntryLineOf(int from)
        {
            int insertAt = from;

            for (int i = from; i < _lines.Count; i++)
            {
                string trimmed = _lines[i].Trim();
                if (IsSectionHeader(trimmed)) break;
                if (trimmed.Length == 0 || IsComment(trimmed)) continue;
                if (trimmed.IndexOf('=') <= 0) continue;

                insertAt = i + 1;
            }

            return insertAt;
        }

        private int FindSectionHeaderLine(string section)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                string trimmed = _lines[i].Trim();
                if (!IsSectionHeader(trimmed)) continue;

                if (string.Equals(SectionNameOf(trimmed), section, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        #endregion
    }
}
