using System;
using System.Text;

namespace TinyConfig
{
    /// <summary>
    /// Obsolete entry point. Shares its name with the namespace, so use <see cref="Config"/>.
    /// </summary>
    [Obsolete("Use Config instead. This type shares its name with the namespace, so `using TinyConfig;` cannot resolve it.")]
    public static class TinyConfig
    {
        /// <inheritdoc cref="Config.FromFile(string)"/>
        public static ITinyConfig FromFile(string filePath) => Config.FromFile(filePath);

        /// <inheritdoc cref="Config.FromFile(string, Encoding)"/>
        public static ITinyConfig FromFile(string filePath, Encoding encoding) => Config.FromFile(filePath, encoding);

        /// <inheritdoc cref="Config.FromJson(string)"/>
        public static ITinyConfig FromJson(string filePath) => Config.FromJson(filePath);

        /// <inheritdoc cref="Config.FromXml(string)"/>
        public static ITinyConfig FromXml(string filePath) => Config.FromXml(filePath);

        /// <inheritdoc cref="Config.FromRegistry(string, RegistryRoot)"/>
        public static ITinyConfig FromRegistry(string subKey, RegistryRoot root = RegistryRoot.CurrentUser)
            => Config.FromRegistry(subKey, root);
    }
}
