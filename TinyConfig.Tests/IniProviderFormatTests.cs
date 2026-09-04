using System.IO;
using System.Text;
using NUnit.Framework;
using TinyConfig;

namespace TinyConfig.Tests
{
    /// <summary>Writing a value keeps comments, blank lines, key order and spacing.</summary>
    [TestFixture]
    public class IniProviderFormatTests
    {
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        private ITinyConfig Given(params string[] lines)
        {
            File.WriteAllLines(_tempFile, lines);
            return Config.FromFile(_tempFile);
        }

        private string[] Actual()
        {
            return File.ReadAllLines(_tempFile);
        }

        [Test]
        public void Set_keeps_comments_and_blank_lines()
        {
            var config = Given(
                "; server settings",
                "[Server]",
                "Host=localhost",
                "",
                "# port is fixed",
                "Port=8080");

            config.Set("Server", "Host", "192.168.0.1");

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "; server settings",
                "[Server]",
                "Host=192.168.0.1",
                "",
                "# port is fixed",
                "Port=8080"
            }));
        }

        [Test]
        public void Set_keeps_spacing_around_separator()
        {
            var config = Given(
                "[Server]",
                "  Host = localhost");

            config.Set("Server", "Host", "192.168.0.1");

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "[Server]",
                "  Host = 192.168.0.1"
            }));
        }

        [Test]
        public void Set_new_key_goes_into_its_own_section()
        {
            var config = Given(
                "[Server]",
                "Host=localhost",
                "",
                "[App]",
                "Debug=true");

            config.Set("Server", "Port", 8080);

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "[Server]",
                "Host=localhost",
                "Port=8080",
                "",
                "[App]",
                "Debug=true"
            }));
        }

        [Test]
        public void Set_new_key_lands_above_trailing_comment()
        {
            var config = Given(
                "[Server]",
                "Host=localhost",
                "; TODO: add timeout");

            config.Set("Server", "Port", 8080);

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "[Server]",
                "Host=localhost",
                "Port=8080",
                "; TODO: add timeout"
            }));
        }

        [Test]
        public void Set_unknown_section_is_appended_with_header()
        {
            var config = Given(
                "[Server]",
                "Host=localhost");

            config.Set("Logging", "Level", "Debug");

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "[Server]",
                "Host=localhost",
                "",
                "[Logging]",
                "Level=Debug"
            }));
        }

        [Test]
        public void Explicit_encoding_is_used_for_read_and_write()
        {
            Encoding utf16 = Encoding.Unicode;
            File.WriteAllLines(_tempFile, new[] { "[CLIENT]", "ENV=\uc6b4\uc601" }, utf16);

            var config = Config.FromFile(_tempFile, utf16);
            Assert.That(config.Get("CLIENT", "ENV"), Is.EqualTo("\uc6b4\uc601"));

            config.Set("CLIENT", "ENV", "\ud14c\uc2a4\ud2b8");

            Assert.That(File.ReadAllLines(_tempFile, utf16),
                Is.EqualTo(new[] { "[CLIENT]", "ENV=\ud14c\uc2a4\ud2b8" }));
        }

        [Test]
        public void Default_encoding_is_utf8_without_bom()
        {
            File.Delete(_tempFile);

            var config = Config.FromFile(_tempFile);
            config.Set("CLIENT", "ENV", "\uc6b4\uc601");

            byte[] bytes = File.ReadAllBytes(_tempFile);
            Assert.That(bytes[0], Is.EqualTo((byte)'['), "UTF-8 BOM should not be written");
            Assert.That(File.ReadAllText(_tempFile, new UTF8Encoding(false)), Does.Contain("\uc6b4\uc601"));
        }

        [Test]
        public void Keys_before_any_header_stay_in_place()
        {
            var config = Given(
                "Legacy=1",
                "",
                "[Server]",
                "Host=localhost");

            Assert.That(config.Get("Default", "Legacy"), Is.EqualTo("1"));

            config.Set("Server", "Host", "192.168.0.1");

            Assert.That(Actual(), Is.EqualTo(new[]
            {
                "Legacy=1",
                "",
                "[Server]",
                "Host=192.168.0.1"
            }));
        }
    }
}
