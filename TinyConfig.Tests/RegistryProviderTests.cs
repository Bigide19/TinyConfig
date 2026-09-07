using System;
using System.Runtime.Versioning;
using NUnit.Framework;
using TinyConfig;
using TinyConfig.Providers;

namespace TinyConfig.Tests
{
    [TestFixture]
    [Platform("Win")]           // NUnit: 다른 OS 에서는 실행하지 않습니다
    [SupportedOSPlatform("windows")]  // 컴파일러: 같은 제약을 분석기에 알립니다
    public class RegistryProviderTests
    {
        private const string TestSubKey = @"SOFTWARE\TinyConfigTest";
        private ITinyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = Config.FromRegistry(TestSubKey);
        }

        [TearDown]
        public void TearDown()
        {
#pragma warning disable CA1416
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(TestSubKey, false);
            }
            catch { }
#pragma warning restore CA1416
        }

        [Test]
        public void Set_and_Get_returns_stored_value()
        {
            _config.Set("General", "Name", "TinyConfig");

            Assert.That(_config.Get("General", "Name"), Is.EqualTo("TinyConfig"));
        }

        [Test]
        public void Get_missing_key_returns_default()
        {
            string result = _config.Get("General", "Missing", "fallback");

            Assert.That(result, Is.EqualTo("fallback"));
        }

        [Test]
        public void Get_generic_int()
        {
            _config.Set("Settings", "Count", 42);

            int result = _config.Get<int>("Settings", "Count", 0);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void Get_generic_bool()
        {
            _config.Set("Settings", "Enabled", true);

            bool result = _config.Get<bool>("Settings", "Enabled", false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Exists_returns_false_for_missing_key()
        {
            Assert.That(_config.Exists("General", "Nope"), Is.False);
        }

        [Test]
        public void Exists_returns_true_after_set()
        {
            _config.Set("General", "Key", "value");

            Assert.That(_config.Exists("General", "Key"), Is.True);
        }

        [Test]
        public void AutoSave_writes_default_when_key_missing()
        {
            string result = _config.Get("App", "Theme", "dark", autoSave: true);

            Assert.That(result, Is.EqualTo("dark"));
            Assert.That(_config.Exists("App", "Theme"), Is.True);
        }

        [Test]
        public void Multiple_sections_are_independent()
        {
            _config.Set("A", "Key", "valueA");
            _config.Set("B", "Key", "valueB");

            Assert.That(_config.Get("A", "Key"), Is.EqualTo("valueA"));
            Assert.That(_config.Get("B", "Key"), Is.EqualTo("valueB"));
        }
    }
}
