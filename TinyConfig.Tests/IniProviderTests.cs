using System;
using System.IO;
using NUnit.Framework;
using TinyConfig;
using TinyConfig.Providers;

namespace TinyConfig.Tests
{
    [TestFixture]
    public class IniProviderTests
    {
        private string _tempFile;
        private ITinyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.GetTempFileName();
            File.Delete(_tempFile);
            _config = Config.FromFile(_tempFile);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        [Test]
        public void Set_and_Get_returns_stored_value()
        {
            _config.Set("Server", "Host", "localhost");

            Assert.That(_config.Get("Server", "Host"), Is.EqualTo("localhost"));
        }

        [Test]
        public void Get_missing_key_returns_default()
        {
            string result = _config.Get("Server", "Host", "fallback");

            Assert.That(result, Is.EqualTo("fallback"));
        }

        [Test]
        public void Get_generic_int()
        {
            _config.Set("Server", "Port", 8080);

            int port = _config.Get<int>("Server", "Port", 0);

            Assert.That(port, Is.EqualTo(8080));
        }

        [Test]
        public void Get_generic_bool()
        {
            _config.Set("App", "Debug", true);

            bool debug = _config.Get<bool>("App", "Debug", false);

            Assert.That(debug, Is.True);
        }

        [Test]
        public void Get_generic_missing_key_returns_default()
        {
            int result = _config.Get<int>("App", "Missing", 42);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void AutoSave_writes_default_when_key_missing()
        {
            string result = _config.Get("App", "Theme", "dark", autoSave: true);

            Assert.That(result, Is.EqualTo("dark"));
            Assert.That(_config.Exists("App", "Theme"), Is.True);
            Assert.That(_config.Get("App", "Theme"), Is.EqualTo("dark"));
        }

        [Test]
        public void Exists_returns_false_for_missing_key()
        {
            Assert.That(_config.Exists("App", "Nope"), Is.False);
        }

        [Test]
        public void Exists_returns_true_after_set()
        {
            _config.Set("App", "Key", "value");

            Assert.That(_config.Exists("App", "Key"), Is.True);
        }

        [Test]
        public void Data_persists_to_file_and_reloads()
        {
            _config.Set("DB", "Host", "127.0.0.1");
            _config.Set("DB", "Port", 5432);

            var reloaded = Config.FromFile(_tempFile);

            Assert.That(reloaded.Get("DB", "Host"), Is.EqualTo("127.0.0.1"));
            Assert.That(reloaded.Get<int>("DB", "Port", 0), Is.EqualTo(5432));
        }

        [Test]
        public void Multiple_sections_are_independent()
        {
            _config.Set("A", "Key", "valueA");
            _config.Set("B", "Key", "valueB");

            Assert.That(_config.Get("A", "Key"), Is.EqualTo("valueA"));
            Assert.That(_config.Get("B", "Key"), Is.EqualTo("valueB"));
        }

        [Test]
        public void Section_and_key_are_case_insensitive()
        {
            _config.Set("Server", "Host", "localhost");

            Assert.That(_config.Get("SERVER", "HOST"), Is.EqualTo("localhost"));
            Assert.That(_config.Get("server", "host"), Is.EqualTo("localhost"));
        }

        [Test]
        public void Loads_existing_ini_file()
        {
            string content = "[General]\nLanguage=ko\nVersion=2\n";
            File.WriteAllText(_tempFile, content);

            var config = Config.FromFile(_tempFile);

            Assert.That(config.Get("General", "Language"), Is.EqualTo("ko"));
            Assert.That(config.Get<int>("General", "Version", 0), Is.EqualTo(2));
        }

        [Test]
        public void Comments_are_ignored_on_load()
        {
            string content = "[App]\n; this is a comment\nMode=release\n";
            File.WriteAllText(_tempFile, content);

            var config = Config.FromFile(_tempFile);

            Assert.That(config.Get("App", "Mode"), Is.EqualTo("release"));
            Assert.That(config.Exists("App", "; this is a comment"), Is.False);
        }
    }
}
