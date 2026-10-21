using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TinyConfig;
using TinyConfig.Internal;

namespace TinyConfig.Tests
{
    /// <summary>Covers encoding detection for INI files opened without an explicit encoding.</summary>
    [TestFixture]
    public class IniEncodingTests
    {
        private const string Original = "; 키오스크 설정\n[Printer]\nName=삼성 SL-M2020\nPort=USB001";

        private string _tempFile;
        private CultureInfo _culture;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ini");
            _culture = CultureInfo.CurrentCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _culture;
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        /// <summary>Returns the bytes File.WriteAllLines produces for the given text on this platform.</summary>
        private static byte[] FileBytes(Encoding encoding, string text)
        {
            string platformText = text.Replace("\n", Environment.NewLine) + Environment.NewLine;
            return encoding.GetPreamble().Concat(encoding.GetBytes(platformText)).ToArray();
        }

        private static Encoding Cp949()
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(949);
        }

        [Test]
        public void Utf8_file_without_bom_stays_without_bom()
        {
            Encoding utf8 = new UTF8Encoding(false);
            File.WriteAllBytes(_tempFile, FileBytes(utf8, Original));

            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Name", "엡손 TM-T82");

            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(utf8, Original.Replace("삼성 SL-M2020", "엡손 TM-T82"))));
        }

        [Test]
        public void Utf8_bom_is_kept_on_write()
        {
            Encoding utf8Bom = new UTF8Encoding(true);
            File.WriteAllBytes(_tempFile, FileBytes(utf8Bom, Original));

            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Port", "USB002");

            Assert.That(config.Get("Printer", "Name"), Is.EqualTo("삼성 SL-M2020"));
            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(utf8Bom, Original.Replace("USB001", "USB002"))));
        }

        [Test]
        public void Utf16_file_reads_and_keeps_its_encoding()
        {
            Encoding utf16 = new UnicodeEncoding(false, true);
            File.WriteAllBytes(_tempFile, FileBytes(utf16, Original));

            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Name", "엡손 TM-T82");

            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(utf16, Original.Replace("삼성 SL-M2020", "엡손 TM-T82"))));
        }

        [Test]
        [Platform(Exclude = "Win", Reason = "Windows uses the system code page before the culture's")]
        public void Ansi_file_reads_with_culture_code_page_off_windows()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
            File.WriteAllBytes(_tempFile, FileBytes(Cp949(), Original));

            var config = Config.FromFile(_tempFile);

            Assert.That(config.Get("Printer", "Name"), Is.EqualTo("삼성 SL-M2020"));
        }

        [Test]
        [Platform(Exclude = "Win", Reason = "Windows uses the system code page before the culture's")]
        public void Ansi_file_stays_in_its_code_page_after_korean_write()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
            File.WriteAllBytes(_tempFile, FileBytes(Cp949(), Original));

            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Name", "엡손 TM-T82");

            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(Cp949(), Original.Replace("삼성 SL-M2020", "엡손 TM-T82"))));
        }

        [Test]
        public void Unset_lines_keep_their_bytes_under_a_single_byte_code_page()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            File.WriteAllBytes(_tempFile, FileBytes(Cp949(), Original));

            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Port", "USB002");

            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(Cp949(), Original.Replace("USB001", "USB002"))));
        }

        [Test]
        public void New_file_is_created_as_utf8_without_bom()
        {
            var config = Config.FromFile(_tempFile);
            config.Set("Printer", "Name", "삼성");

            Assert.That(File.ReadAllBytes(_tempFile),
                Is.EqualTo(FileBytes(new UTF8Encoding(false), "[Printer]\nName=삼성")));
        }

        [Test]
        public void Explicit_encoding_skips_detection()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
            File.WriteAllBytes(_tempFile, FileBytes(Cp949(), Original));

            var config = Config.FromFile(_tempFile, new UTF8Encoding(false));

            Assert.That(config.Get("Printer", "Name"), Does.Contain("�"));
        }

        [TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 65001)]
        [TestCase(new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, 1200)]
        [TestCase(new byte[] { 0xFE, 0xFF, 0x00, 0x41 }, 1201)]
        [TestCase(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 }, 12000)]
        [TestCase(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 }, 12001)]
        public void Byte_order_mark_picks_its_encoding(byte[] bytes, int codePage)
        {
            Encoding detected = EncodingDetector.Detect(bytes);

            Assert.That(detected.CodePage, Is.EqualTo(codePage));
            Assert.That(detected.GetPreamble(), Is.Not.Empty);
        }

        [Test]
        [Platform(Exclude = "Win", Reason = "Windows uses the system code page before the culture's")]
        public void Content_no_code_page_decodes_falls_back_to_latin1()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
            byte[] bytes = Cp949().GetBytes("Name=삼성").Concat(new byte[] { 0xB0 }).ToArray();

            Encoding detected = EncodingDetector.Detect(bytes);

            Assert.That(detected.CodePage, Is.EqualTo(28591));
        }

        [Test]
        public void Empty_file_is_utf8_without_bom()
        {
            Encoding detected = EncodingDetector.Detect(new byte[0]);

            Assert.That(detected.CodePage, Is.EqualTo(65001));
            Assert.That(detected.GetPreamble(), Is.Empty);
        }
    }
}
