using System.IO;
using NUnit.Framework;
using TinyConfig;

namespace TinyConfig.Tests
{
    /// <summary>The obsolete entry point still delegates to <see cref="Config"/>.</summary>
    [TestFixture]
    public class ObsoleteEntryPointTests
    {
        [Test]
        public void Obsolete_TinyConfig_still_delegates_to_Config()
        {
            string path = Path.GetTempFileName();
            File.Delete(path);

            try
            {
#pragma warning disable CS0618
                ITinyConfig config = TinyConfig.FromFile(path);
#pragma warning restore CS0618

                config.Set("Server", "Host", "localhost");

                Assert.That(config.Get("Server", "Host"), Is.EqualTo("localhost"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
