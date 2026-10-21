using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TinyConfig.Internal
{
    /// <summary>
    /// Picks the encoding of a text file that carries no encoding declaration.
    /// Order: byte order mark, strict UTF-8, the system ANSI code page, the current
    /// culture's ANSI code page, then Latin-1. Every code page candidate must decode the
    /// whole file without errors, and Latin-1 maps each byte to one character, so writing
    /// back with the picked encoding keeps unchanged lines byte for byte.
    /// </summary>
    internal static class EncodingDetector
    {
        private const int Utf8CodePage = 65001;
        private const int Latin1CodePage = 28591;

        /// <summary>Detects the encoding of the file at the given path. A missing or empty file is UTF-8 without BOM.</summary>
        public static Encoding Detect(string filePath)
        {
            if (!File.Exists(filePath)) return new UTF8Encoding(false);
            return Detect(File.ReadAllBytes(filePath));
        }

        /// <summary>Detects the encoding of the given file content.</summary>
        public static Encoding Detect(byte[] bytes)
        {
            Encoding bom = FromByteOrderMark(bytes);
            if (bom != null) return bom;

            if (CanDecode(new UTF8Encoding(false), bytes)) return new UTF8Encoding(false);

            Encoding system = FromCodePage(SystemAnsiCodePage());
            if (system != null && CanDecode(system, bytes)) return system;

            Encoding culture = FromCodePage(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
            if (culture != null && CanDecode(culture, bytes)) return culture;

            return Encoding.GetEncoding(Latin1CodePage);
        }

        /// <summary>Returns the encoding named by a leading byte order mark, or null when there is none.</summary>
        private static Encoding FromByteOrderMark(byte[] b)
        {
            if (StartsWith(b, 0xEF, 0xBB, 0xBF)) return new UTF8Encoding(true);
            if (StartsWith(b, 0xFF, 0xFE, 0x00, 0x00)) return new UTF32Encoding(false, true);
            if (StartsWith(b, 0x00, 0x00, 0xFE, 0xFF)) return new UTF32Encoding(true, true);
            if (StartsWith(b, 0xFF, 0xFE)) return new UnicodeEncoding(false, true);
            if (StartsWith(b, 0xFE, 0xFF)) return new UnicodeEncoding(true, true);
            return null;
        }

        private static bool StartsWith(byte[] bytes, params byte[] prefix)
        {
            if (bytes.Length < prefix.Length) return false;

            for (int i = 0; i < prefix.Length; i++)
                if (bytes[i] != prefix[i]) return false;

            return true;
        }

        /// <summary>Returns true when the whole content decodes without a single invalid sequence.</summary>
        private static bool CanDecode(Encoding encoding, byte[] bytes)
        {
            Encoding strict = (Encoding)encoding.Clone();
            strict.DecoderFallback = DecoderFallback.ExceptionFallback;

            try
            {
                strict.GetCharCount(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the encoding for an ANSI code page, or null when the code page is UTF-8,
        /// unknown, or not available on this runtime. On netstandard2.0 running on .NET Core,
        /// code pages are available only after the caller registers CodePagesEncodingProvider.
        /// </summary>
        private static Encoding FromCodePage(int codePage)
        {
            if (codePage <= 0 || codePage == Utf8CodePage) return null;

#if NETCOREAPP
            Encoding provided = CodePagesEncodingProvider.Instance.GetEncoding(codePage);
            if (provided != null) return provided;
#endif

            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the system ANSI code page that the Win32 INI API uses, or 0 off Windows.
        /// </summary>
        private static int SystemAnsiCodePage()
        {
#if NETFRAMEWORK
            return Encoding.Default.CodePage;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GetACP() : 0;
#endif
        }

#if !NETFRAMEWORK
        [DllImport("kernel32.dll")]
        private static extern int GetACP();
#endif
    }
}
