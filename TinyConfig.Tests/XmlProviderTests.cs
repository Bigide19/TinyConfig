using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using TinyConfig;

namespace TinyConfig.Tests
{
    [TestFixture]
    public class XmlProviderTests
    {
        private string _tempFile;
        private ITinyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
            _config = TinyConfig.FromXml(_tempFile);
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
            _config.Set("Server", "Port", 9090);

            Assert.That(_config.Get<int>("Server", "Port", 0), Is.EqualTo(9090));
        }

        [Test]
        public void Get_generic_bool()
        {
            _config.Set("App", "Enabled", true);

            Assert.That(_config.Get<bool>("App", "Enabled", false), Is.True);
        }

        [Test]
        public void AutoSave_writes_default_when_key_missing()
        {
            string result = _config.Get("App", "Region", "US", autoSave: true);

            Assert.That(result, Is.EqualTo("US"));
            Assert.That(_config.Exists("App", "Region"), Is.True);
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
            _config.Set("DB", "Host", "10.0.0.1");
            _config.Set("DB", "Port", 3306);

            var reloaded = TinyConfig.FromXml(_tempFile);

            Assert.That(reloaded.Get("DB", "Host"), Is.EqualTo("10.0.0.1"));
            Assert.That(reloaded.Get<int>("DB", "Port", 0), Is.EqualTo(3306));
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
        public void Loads_existing_xml_file()
        {
            string xml = "<?xml version=\"1.0\"?><Config><General><Language>ko</Language><Version>2</Version></General></Config>";
            File.WriteAllText(_tempFile, xml);

            var config = TinyConfig.FromXml(_tempFile);

            Assert.That(config.Get("General", "Language"), Is.EqualTo("ko"));
            Assert.That(config.Get<int>("General", "Version", 0), Is.EqualTo(2));
        }

        [Test]
        public void Saved_file_is_valid_xml()
        {
            _config.Set("Test", "Key", "value");

            string content = File.ReadAllText(_tempFile);
            var doc = new XmlDocument();
            Assert.DoesNotThrow(() => doc.LoadXml(content));
        }
    }
}
