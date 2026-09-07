using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TinyConfig.Internal
{
    /// <summary>
    /// Minimal JSON reader and writer for the two-level shape TinyConfig stores:
    /// <c>{ "Section": { "Key": value } }</c>.
    /// <para>
    /// Replaces System.Text.Json so the library carries no package dependency and
    /// works on .NET Framework 4.6.1, where System.Text.Json 8.x is unsupported.
    /// Reading stays permissive because config files get hand-edited; writing emits
    /// the narrow subset above.
    /// </para>
    /// </summary>
    internal static class MiniJson
    {
        // Values are always handed back as strings. The mapping mirrors what
        // JsonElement.ToString() used to return, so files written by 1.2.0 and
        // earlier keep reading back identically:
        //   "text" -> text (unescaped)   123 -> 123 (raw, not normalized)
        //   true   -> True               null -> "" (empty)
        // The capitalized True/False look odd for JSON, but Boolean.ToString()
        // produces them and Set<bool> has always stored them that way.
        private const string TrueText = "True";
        private const string FalseText = "False";

        /// <summary>
        /// Parses a JSON object into section/key/value form. Non-object section
        /// values are skipped, matching the previous provider behavior.
        /// </summary>
        /// <exception cref="FormatException">The text is not a JSON object.</exception>
        public static Dictionary<string, Dictionary<string, string>> Parse(string text)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return result;

            int i = 0;
            SkipByteOrderMark(text, ref i);
            SkipWhitespace(text, ref i);

            // An empty file is a valid starting point, not a parse error.
            if (i >= text.Length) return result;

            Expect(text, ref i, '{');
            SkipWhitespace(text, ref i);

            if (Peek(text, i) == '}') return result;

            while (true)
            {
                SkipWhitespace(text, ref i);
                string sectionName = ReadString(text, ref i);

                SkipWhitespace(text, ref i);
                Expect(text, ref i, ':');
                SkipWhitespace(text, ref i);

                if (Peek(text, i) == '{')
                {
                    result[sectionName] = ReadFlatObject(text, ref i);
                }
                else
                {
                    // Top-level scalars and arrays carry no section, so drop them.
                    SkipValue(text, ref i);
                }

                SkipWhitespace(text, ref i);
                char next = Read(text, ref i);
                if (next == ',') continue;
                if (next == '}') break;
                throw Unexpected(next, i);
            }

            return result;
        }

        /// <summary>
        /// Writes section/key/value form as indented JSON: two spaces, LF, no BOM.
        /// Escaping is kept to what the JSON grammar requires, so non-ASCII text
        /// stays readable as UTF-8.
        /// </summary>
        public static string Write(Dictionary<string, Dictionary<string, string>> data)
        {
            if (data.Count == 0) return "{}";

            var sb = new StringBuilder();
            sb.Append("{\n");

            bool firstSection = true;
            foreach (var section in data)
            {
                if (!firstSection) sb.Append(",\n");
                firstSection = false;

                sb.Append("  ");
                AppendString(sb, section.Key);

                if (section.Value.Count == 0)
                {
                    sb.Append(": {}");
                    continue;
                }

                sb.Append(": {\n");

                bool firstKey = true;
                foreach (var entry in section.Value)
                {
                    if (!firstKey) sb.Append(",\n");
                    firstKey = false;

                    sb.Append("    ");
                    AppendString(sb, entry.Key);
                    sb.Append(": ");
                    AppendString(sb, entry.Value);
                }

                sb.Append('\n');
                sb.Append("  }");
            }

            if (!firstSection) sb.Append('\n');
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>Reads one section object, converting each member value to a string.</summary>
        private static Dictionary<string, string> ReadFlatObject(string text, ref int i)
        {
            var section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Expect(text, ref i, '{');
            SkipWhitespace(text, ref i);

            if (Peek(text, i) == '}')
            {
                i++;
                return section;
            }

            while (true)
            {
                SkipWhitespace(text, ref i);
                string key = ReadString(text, ref i);

                SkipWhitespace(text, ref i);
                Expect(text, ref i, ':');
                SkipWhitespace(text, ref i);

                section[key] = ReadValueAsText(text, ref i);

                SkipWhitespace(text, ref i);
                char next = Read(text, ref i);
                if (next == ',') continue;
                if (next == '}') break;
                throw Unexpected(next, i);
            }

            return section;
        }

        /// <summary>Reads any JSON value and renders it the way the old provider did.</summary>
        private static string ReadValueAsText(string text, ref int i)
        {
            char c = Peek(text, i);

            if (c == '"') return ReadString(text, ref i);

            if (c == '{' || c == '[')
            {
                // Nested containers are outside the two-level model. Hand back the
                // raw slice rather than re-serializing it.
                int start = i;
                SkipValue(text, ref i);
                return text.Substring(start, i - start);
            }

            if (c == 't')
            {
                ExpectLiteral(text, ref i, "true");
                return TrueText;
            }

            if (c == 'f')
            {
                ExpectLiteral(text, ref i, "false");
                return FalseText;
            }

            if (c == 'n')
            {
                ExpectLiteral(text, ref i, "null");
                return string.Empty;
            }

            return ReadNumber(text, ref i);
        }

        /// <summary>Reads a quoted string and resolves its escape sequences.</summary>
        private static string ReadString(string text, ref int i)
        {
            Expect(text, ref i, '"');
            var sb = new StringBuilder();

            while (true)
            {
                if (i >= text.Length) throw new FormatException("Unterminated string in JSON.");
                char c = text[i++];

                if (c == '"') break;

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (i >= text.Length) throw new FormatException("Truncated escape sequence in JSON.");
                char esc = text[i++];

                switch (esc)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        // Surrogate pairs arrive as two \u escapes; appending each code
                        // unit in order rebuilds the character, since .NET strings are UTF-16.
                        sb.Append(ReadHexCodeUnit(text, ref i));
                        break;
                    default:
                        throw new FormatException("Unknown escape sequence '\\" + esc + "' in JSON.");
                }
            }

            return sb.ToString();
        }

        /// <summary>Reads the four hex digits of a <c>\u</c> escape.</summary>
        private static char ReadHexCodeUnit(string text, ref int i)
        {
            if (i + 4 > text.Length) throw new FormatException("Truncated \\u escape in JSON.");

            string hex = text.Substring(i, 4);
            int code;
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                throw new FormatException("Invalid \\u escape '" + hex + "' in JSON.");

            i += 4;
            return (char)code;
        }

        /// <summary>Reads a number and returns its text as written, without normalizing.</summary>
        private static string ReadNumber(string text, ref int i)
        {
            int start = i;
            if (Peek(text, i) == '-') i++;

            while (i < text.Length)
            {
                char c = text[i];
                bool partOfNumber = (c >= '0' && c <= '9')
                    || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-';
                if (!partOfNumber) break;
                i++;
            }

            if (i == start) throw Unexpected(Peek(text, i), i);
            return text.Substring(start, i - start);
        }

        /// <summary>Advances past one value without interpreting it.</summary>
        private static void SkipValue(string text, ref int i)
        {
            char c = Peek(text, i);

            if (c == '"')
            {
                ReadString(text, ref i);
                return;
            }

            if (c == '{' || c == '[')
            {
                char close = c == '{' ? '}' : ']';
                int depth = 0;

                while (i < text.Length)
                {
                    char cur = text[i];

                    if (cur == '"')
                    {
                        ReadString(text, ref i);
                        continue;
                    }

                    i++;
                    if (cur == c) depth++;
                    else if (cur == close)
                    {
                        depth--;
                        if (depth == 0) return;
                    }
                }

                throw new FormatException("Unterminated object or array in JSON.");
            }

            ReadValueAsText(text, ref i);
        }

        /// <summary>Appends a JSON string literal, escaping only what the grammar requires.</summary>
        private static void AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');

            if (value != null)
            {
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            // The grammar only forbids raw control characters. Everything
                            // else, non-ASCII included, is written through as UTF-8.
                            if (c < ' ')
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }

            sb.Append('"');
        }

        private static void SkipByteOrderMark(string text, ref int i)
        {
            if (i < text.Length && text[i] == '\uFEFF') i++;
        }

        private static void SkipWhitespace(string text, ref int i)
        {
            while (i < text.Length)
            {
                char c = text[i];
                if (c != ' ' && c != '\t' && c != '\n' && c != '\r') break;
                i++;
            }
        }

        private static char Peek(string text, int i)
        {
            return i < text.Length ? text[i] : '\0';
        }

        private static char Read(string text, ref int i)
        {
            if (i >= text.Length) throw new FormatException("Unexpected end of JSON.");
            return text[i++];
        }

        private static void Expect(string text, ref int i, char expected)
        {
            char actual = Read(text, ref i);
            if (actual != expected)
                throw new FormatException("Expected '" + expected + "' at offset " + (i - 1) + " but found '" + actual + "'.");
        }

        private static void ExpectLiteral(string text, ref int i, string literal)
        {
            if (i + literal.Length > text.Length || string.CompareOrdinal(text, i, literal, 0, literal.Length) != 0)
                throw new FormatException("Expected '" + literal + "' at offset " + i + ".");
            i += literal.Length;
        }

        private static FormatException Unexpected(char c, int i)
        {
            return new FormatException("Unexpected character '" + c + "' at offset " + (i - 1) + " in JSON.");
        }
    }
}
