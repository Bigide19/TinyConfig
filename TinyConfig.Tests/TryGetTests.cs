using System;
using System.IO;
using NUnit.Framework;
using TinyConfig;

namespace TinyConfig.Tests
{
    /// <summary>TryGet separates a match, a missing key, and text that does not fit the type.</summary>
    [TestFixture]
    public class TryGetTests
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
        public void TryGet_returns_true_when_type_matches()
        {
            _config.Set("Server", "Port", 8080);

            int port;
            Assert.That(_config.TryGet("Server", "Port", out port), Is.True);
            Assert.That(port, Is.EqualTo(8080));
        }

        [Test]
        public void TryGet_returns_false_when_key_is_missing()
        {
            int port;

            Assert.That(_config.TryGet("Server", "Port", out port), Is.False);
            Assert.That(port, Is.EqualTo(0));
        }

        [Test]
        public void TryGet_returns_false_when_value_is_empty()
        {
            _config.Set("Server", "Host", "");

            string host;
            Assert.That(_config.TryGet("Server", "Host", out host), Is.False);
        }

        [Test]
        public void TryGet_returns_false_when_text_does_not_fit_the_type()
        {
            _config.Set("Shutdown", "Everyday", "210000");

            DateTime time;
            Assert.That(_config.TryGet("Shutdown", "Everyday", out time), Is.False);
        }

        [Test]
        public void Get_generic_still_falls_back_for_the_same_text()
        {
            _config.Set("Shutdown", "Everyday", "210000");
            DateTime fallback = new DateTime(2000, 1, 1);

            Assert.That(_config.Get("Shutdown", "Everyday", fallback), Is.EqualTo(fallback));
        }

        [Test]
        public void Get_generic_autoSaves_when_value_is_empty()
        {
            _config.Set("App", "Retry", "");

            int retry = _config.Get("App", "Retry", 3, autoSave: true);

            Assert.That(retry, Is.EqualTo(3));
            Assert.That(_config.Get("App", "Retry"), Is.EqualTo("3"));
        }
    }
}
