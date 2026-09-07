using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TinyConfig;
using TinyConfig.Internal;

namespace TinyConfig.Tests
{
    /// <summary>
    /// Covers the hand-written JSON reader and writer that replaced System.Text.Json.
    /// The existing JsonProviderTests store every value as a string, so the escape,
    /// number and boolean paths only get exercised here.
    /// </summary>
    [TestFixture]
    public class MiniJsonTests
    {
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        // ---------------------------------------------------------------
        // Reading: value kinds other than string
        // ---------------------------------------------------------------

        [Test]
        public void Reads_number_as_written_without_normalizing()
        {
            var data = MiniJson.Parse("{\"S\":{\"int\":5432,\"dec\":1.50,\"exp\":1e3,\"neg\":-7}}");

            Assert.That(data["S"]["int"], Is.EqualTo("5432"));
            Assert.That(data["S"]["dec"], Is.EqualTo("1.50"));
            Assert.That(data["S"]["exp"], Is.EqualTo("1e3"));
            Assert.That(data["S"]["neg"], Is.EqualTo("-7"));
        }

        [Test]
        public void Reads_boolean_capitalized_like_Boolean_ToString()
        {
            // Set<bool> stores Boolean.ToString(), so a hand-edited `true` has to read
            // back the same way or Get(section, key) would disagree with Get<bool>.
            var data = MiniJson.Parse("{\"S\":{\"t\":true,\"f\":false}}");

            Assert.That(data["S"]["t"], Is.EqualTo("True"));
            Assert.That(data["S"]["f"], Is.EqualTo("False"));
        }

        [Test]
        public void Reads_null_as_empty_string()
        {
            var data = MiniJson.Parse("{\"S\":{\"nul\":null}}");

            Assert.That(data["S"]["nul"], Is.EqualTo(string.Empty));
        }

        [Test]
        public void Reads_nested_container_as_raw_text()
        {
            var data = MiniJson.Parse("{\"S\":{\"obj\":{\"k\":1},\"arr\":[1,2]}}");

            Assert.That(data["S"]["obj"], Is.EqualTo("{\"k\":1}"));
            Assert.That(data["S"]["arr"], Is.EqualTo("[1,2]"));
        }

        [Test]
        public void Skips_section_whose_value_is_not_an_object()
        {
            var data = MiniJson.Parse("{\"scalar\":1,\"arr\":[1,2],\"S\":{\"k\":\"v\"}}");

            Assert.That(data.ContainsKey("scalar"), Is.False);
            Assert.That(data.ContainsKey("arr"), Is.False);
            Assert.That(data["S"]["k"], Is.EqualTo("v"));
        }

        // ---------------------------------------------------------------
        // Reading: escape sequences
        // ---------------------------------------------------------------

        [Test]
        public void Reads_unicode_escape_written_by_earlier_versions()
        {
            // 1.2.0 and earlier wrote non-ASCII through Utf8JsonWriter, which escapes
            // every code unit. Those files have to keep reading back correctly.
            var data = MiniJson.Parse("{\"General\":{\"Lang\":\"\\uD55C\\uAE00\"}}");

            Assert.That(data["General"]["Lang"], Is.EqualTo("한글"));
        }

        [Test]
        public void Reads_surrogate_pair_escape()
        {
            // U+1F600 arrives as two \u escapes; appending both code units rebuilds it.
            var data = MiniJson.Parse("{\"S\":{\"emoji\":\"\\uD83D\\uDE00\"}}");

            Assert.That(data["S"]["emoji"], Is.EqualTo("\U0001F600"));
        }

        [Test]
        public void Reads_short_form_escapes()
        {
            var data = MiniJson.Parse("{\"S\":{\"k\":\"a\\\"b\\\\c\\/d\\be\\ff\\ng\\rh\\ti\"}}");

            Assert.That(data["S"]["k"], Is.EqualTo("a\"b\\c/d\be\ff\ng\rh\ti"));
        }

        [Test]
        public void Reads_escaped_quote_in_key()
        {
            var data = MiniJson.Parse("{\"S\":{\"a\\\"b\":\"v\"}}");

            Assert.That(data["S"]["a\"b"], Is.EqualTo("v"));
        }

        // ---------------------------------------------------------------
        // Reading: shape and whitespace tolerance
        // ---------------------------------------------------------------

        [Test]
        public void Reads_empty_text_as_no_sections()
        {
            Assert.That(MiniJson.Parse("").Count, Is.EqualTo(0));
            Assert.That(MiniJson.Parse("   \n\t ").Count, Is.EqualTo(0));
        }

        [Test]
        public void Reads_empty_object()
        {
            Assert.That(MiniJson.Parse("{}").Count, Is.EqualTo(0));
        }

        [Test]
        public void Reads_empty_section()
        {
            var data = MiniJson.Parse("{\"S\":{}}");

            Assert.That(data.ContainsKey("S"), Is.True);
            Assert.That(data["S"].Count, Is.EqualTo(0));
        }

        [Test]
        public void Skips_byte_order_mark()
        {
            var data = MiniJson.Parse("\uFEFF{\"S\":{\"k\":\"v\"}}");

            Assert.That(data["S"]["k"], Is.EqualTo("v"));
        }

        [Test]
        public void Tolerates_whitespace_and_newlines_between_tokens()
        {
            var data = MiniJson.Parse("{\n  \"S\" :\t{\n    \"k\" : \"v\" ,\n    \"k2\":\"v2\"\n  }\n}");

            Assert.That(data["S"]["k"], Is.EqualTo("v"));
            Assert.That(data["S"]["k2"], Is.EqualTo("v2"));
        }

        [Test]
        public void Section_and_key_lookup_ignores_case()
        {
            var data = MiniJson.Parse("{\"General\":{\"Language\":\"ko\"}}");

            Assert.That(data["GENERAL"]["language"], Is.EqualTo("ko"));
        }

        // ---------------------------------------------------------------
        // Reading: malformed input
        // ---------------------------------------------------------------

        [Test]
        public void Throws_FormatException_on_malformed_json()
        {
            // System.Text.Json raised JsonException here; the hand-written parser
            // raises FormatException, which is noted in the 1.3.0 release notes.
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"S\":{\"k\":\"v\""));
            Assert.Throws<FormatException>(() => MiniJson.Parse("[1,2]"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"S\":{\"k\" \"v\"}}"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"S\":{\"k\":tru}}"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"S\":{\"k\":\"\\q\"}}"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"S\":{\"k\":\"\\u12\"}}"));
        }

        // ---------------------------------------------------------------
        // Writing
        // ---------------------------------------------------------------

        [Test]
        public void Writes_two_space_indent_with_lf_newlines()
        {
            var data = Data("Server", "Host", "localhost");

            Assert.That(MiniJson.Write(data), Is.EqualTo("{\n  \"Server\": {\n    \"Host\": \"localhost\"\n  }\n}"));
        }

        [Test]
        public void Writes_non_ascii_as_is()
        {
            // The chosen policy: escape only what the grammar requires, so a config
            // file stays readable. Earlier versions emitted \uD55C\uAE00 here.
            var json = MiniJson.Write(Data("General", "Lang", "한글"));

            Assert.That(json, Does.Contain("\"한글\""));
            Assert.That(json, Does.Not.Contain("\\u"));
        }

        [Test]
        public void Writes_html_sensitive_characters_as_is()
        {
            var json = MiniJson.Write(Data("S", "k", "a&b<c>'d+e"));

            Assert.That(json, Does.Contain("\"a&b<c>'d+e\""));
        }

        [Test]
        public void Escapes_only_what_the_grammar_requires()
        {
            var json = MiniJson.Write(Data("S", "k", "q\"b\\s\tt\nn\rr"));

            Assert.That(json, Does.Contain("\"q\\\"b\\\\s\\tt\\nn\\rr\""));
        }

        [Test]
        public void Escapes_other_control_characters_numerically()
        {
            var json = MiniJson.Write(Data("S", "k", "a\u0001b"));

            Assert.That(json, Does.Contain("a\\u0001b"));
        }

        [Test]
        public void Writes_empty_data_as_empty_object()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();

            Assert.That(MiniJson.Write(data), Is.EqualTo("{}"));
        }

        [Test]
        public void Writes_empty_section_as_empty_object()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            data["S"] = new Dictionary<string, string>();

            Assert.That(MiniJson.Write(data), Is.EqualTo("{\n  \"S\": {}\n}"));
        }

        [Test]
        public void Writes_multiple_sections_and_keys_with_separators()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            data["A"] = new Dictionary<string, string>();
            data["A"]["k1"] = "v1";
            data["A"]["k2"] = "v2";
            data["B"] = new Dictionary<string, string>();
            data["B"]["k"] = "v";

            Assert.That(MiniJson.Write(data), Is.EqualTo(
                "{\n  \"A\": {\n    \"k1\": \"v1\",\n    \"k2\": \"v2\"\n  },\n  \"B\": {\n    \"k\": \"v\"\n  }\n}"));
        }

        // ---------------------------------------------------------------
        // Round trip and provider-level compatibility
        // ---------------------------------------------------------------

        [Test]
        public void Round_trips_awkward_values()
        {
            var data = Data("S", "k", "한글 \"quoted\" \\ back / slash\ttab\nnewline &<>'+ 이모지 \U0001F600");

            var reparsed = MiniJson.Parse(MiniJson.Write(data));

            Assert.That(reparsed["S"]["k"], Is.EqualTo(data["S"]["k"]));
        }

        [Test]
        public void Written_output_parses_with_the_standard_library()
        {
            var json = MiniJson.Write(Data("General", "Lang", "한글 \"q\" \\ &<>'+ \U0001F600"));

            Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(json));
        }

        [Test]
        public void Provider_reads_file_written_by_earlier_versions()
        {
            // Byte-for-byte what 1.2.0 produced for these values.
            string legacy = "{\n  \"General\": {\n    \"Lang\": \"\\uD55C\\uAE00\",\n"
                + "    \"Amp\": \"a\\u0026b\\u003Cc\\u003E\",\n    \"Quote\": \"a\\u0022b\"\n  }\n}";
            File.WriteAllText(_tempFile, legacy, new UTF8Encoding(false));

            var config = Config.FromJson(_tempFile);

            Assert.That(config.Get("General", "Lang"), Is.EqualTo("한글"));
            Assert.That(config.Get("General", "Amp"), Is.EqualTo("a&b<c>"));
            Assert.That(config.Get("General", "Quote"), Is.EqualTo("a\"b"));
        }

        [Test]
        public void Provider_reads_hand_edited_number_and_boolean()
        {
            File.WriteAllText(_tempFile, "{ \"App\": { \"Port\": 8080, \"Debug\": true } }");

            var config = Config.FromJson(_tempFile);

            Assert.That(config.Get<int>("App", "Port", 0), Is.EqualTo(8080));
            Assert.That(config.Get<bool>("App", "Debug", false), Is.True);
        }

        [Test]
        public void Provider_writes_file_without_bom()
        {
            var config = Config.FromJson(_tempFile);
            config.Set("General", "Lang", "한글");

            var bytes = File.ReadAllBytes(_tempFile);

            Assert.That(bytes[0], Is.EqualTo((byte)'{'));
            Assert.That(File.ReadAllText(_tempFile), Does.Contain("한글"));
        }

        [Test]
        public void Provider_round_trips_through_the_file()
        {
            var config = Config.FromJson(_tempFile);
            config.Set("S", "Text", "값 \"인용\"\t탭");

            var reloaded = Config.FromJson(_tempFile);

            Assert.That(reloaded.Get("S", "Text"), Is.EqualTo("값 \"인용\"\t탭"));
        }

        /// <summary>Builds a single-section, single-key structure.</summary>
        private static Dictionary<string, Dictionary<string, string>> Data(string section, string key, string value)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            data[section] = new Dictionary<string, string>();
            data[section][key] = value;
            return data;
        }
    }
}
