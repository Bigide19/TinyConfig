using System;
using System.IO;
using NUnit.Framework;
using TinyConfig;

namespace TinyConfig.Tests
{
    [TestFixture]
    public class JsonProviderTests
    {
        private string _tempFile;
        private ITinyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            _config = Config.FromJson(_tempFile);
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
            Assert.That(_config.Get("Server", "Host", "fallback"), Is.EqualTo("fallback"));
        }

        [Test]
        public void Get_generic_int()
        {
            _config.Set("Server", "Port", 3000);

            Assert.That(_config.Get<int>("Server", "Port", 0), Is.EqualTo(3000));
        }

        [Test]
        public void Get_generic_bool()
        {
            _config.Set("App", "Verbose", true);

            Assert.That(_config.Get<bool>("App", "Verbose", false), Is.True);
        }

        [Test]
        public void AutoSave_writes_default_when_key_missing()
        {
            string result = _config.Get("App", "Lang", "en", autoSave: true);

            Assert.That(result, Is.EqualTo("en"));
            Assert.That(_config.Exists("App", "Lang"), Is.True);
        }

        [Test]
        public void Exists_returns_false_for_missing_key()
        {
            Assert.That(_config.Exists("App", "Nope"), Is.False);
        }

        [Test]
        public void Exists_returns_true_after_set()
        {
            _config.Set("App", "Key", "val");

            Assert.That(_config.Exists("App", "Key"), Is.True);
        }

        [Test]
        public void Data_persists_to_file_and_reloads()
        {
            _config.Set("DB", "Host", "127.0.0.1");
            _config.Set("DB", "Port", 5432);

            var reloaded = Config.FromJson(_tempFile);

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
        public void Loads_existing_json_file()
        {
            string json = "{ \"General\": { \"Language\": \"ko\", \"Version\": \"2\" } }";
            File.WriteAllText(_tempFile, json);

            var config = Config.FromJson(_tempFile);

            Assert.That(config.Get("General", "Language"), Is.EqualTo("ko"));
            Assert.That(config.Get<int>("General", "Version", 0), Is.EqualTo(2));
        }

        [Test]
        public void Saved_file_is_valid_json()
        {
            _config.Set("Test", "Key", "value");

            string content = File.ReadAllText(_tempFile);
            Assert.DoesNotThrow(() => System.Text.Json.JsonDocument.Parse(content));
        }
    }
}
