using System.Text;

namespace TinyConfig
{
    /// <summary>
    /// Entry point for creating configuration providers.
    /// </summary>
    public static class Config
    {
        /// <summary>Creates an INI file provider using UTF-8 without BOM.</summary>
        public static ITinyConfig FromFile(string filePath) => new Providers.IniProvider(filePath);

        /// <summary>Creates an INI file provider with an explicit encoding.</summary>
        /// <param name="encoding">
        /// On .NET Core and later, code pages such as 949 require
        /// Encoding.RegisterProvider(CodePagesEncodingProvider.Instance).
        /// </param>
        public static ITinyConfig FromFile(string filePath, Encoding encoding)
            => new Providers.IniProvider(filePath, encoding);

        /// <summary>Creates a JSON file provider.</summary>
        public static ITinyConfig FromJson(string filePath) => new Providers.JsonProvider(filePath);

        /// <summary>Creates an XML file-based configuration provider.</summary>
        public static ITinyConfig FromXml(string filePath) => new Providers.XmlProvider(filePath);

        /// <summary>Creates a Windows Registry-based configuration provider.</summary>
        /// <param name="subKey">Registry sub-key path (e.g. SOFTWARE\MyApp).</param>
        /// <param name="root">Registry root hive. Defaults to HKEY_CURRENT_USER.</param>
        public static ITinyConfig FromRegistry(string subKey, RegistryRoot root = RegistryRoot.CurrentUser)
            => new Providers.RegistryProvider(subKey, root);
    }
}
