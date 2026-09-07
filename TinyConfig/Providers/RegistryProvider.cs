using TinyConfig.Internal;
using Microsoft.Win32;
using System;
#if !NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

namespace TinyConfig.Providers
{
    /// <summary>
    /// Reads and writes configuration from the Windows Registry.
    /// Sections map to sub-keys under the root path, keys to values within them.
    /// </summary>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public class RegistryProvider : ITinyConfig
    {
        private readonly string _rootSubKey;
        private readonly RegistryKey _hive;

        /// <summary>Creates a provider over the given sub-key.</summary>
        /// <exception cref="PlatformNotSupportedException">Thrown when the OS is not Windows.</exception>
        public RegistryProvider(string subKey, RegistryRoot root = RegistryRoot.CurrentUser)
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException(
                    "The Registry provider requires Windows. Use FromFile, FromJson or FromXml instead.");
#endif

            _rootSubKey = subKey;
            _hive = GetHive(root);
        }

        /// <inheritdoc/>
        public string Get(string section, string key, string defaultValue = "", bool autoSave = false)
        {
            string subKey = BuildSubKey(section);
            using (RegistryKey regKey = _hive.OpenSubKey(subKey))
            {
                object val = regKey?.GetValue(key);
                if (val != null) return val.ToString();
            }

            if (autoSave)
            {
                Set(section, key, defaultValue);
            }

            return defaultValue;
        }

        /// <inheritdoc/>
        public T Get<T>(string section, string key, T defaultValue = default(T), bool autoSave = false)
        {
            string valStr = Get(section, key, null, false);

            if (string.IsNullOrWhiteSpace(valStr))
            {
                if (autoSave) Set(section, key, defaultValue);
                return defaultValue;
            }

            return ValueConverter.Convert(valStr, defaultValue);
        }

        /// <inheritdoc/>
        public bool TryGet<T>(string section, string key, out T value)
        {
            return ValueConverter.TryConvert(Get(section, key, null, false), out value);
        }

        /// <inheritdoc/>
        public void Set<T>(string section, string key, T value)
        {
            string subKey = BuildSubKey(section);
            using (RegistryKey regKey = _hive.CreateSubKey(subKey))
            {
                if (regKey != null)
                {
                    regKey.SetValue(key, value?.ToString() ?? "");
                }
            }
        }

        /// <inheritdoc/>
        public bool Exists(string section, string key)
        {
            string subKey = BuildSubKey(section);
            using (RegistryKey regKey = _hive.OpenSubKey(subKey))
            {
                return regKey?.GetValue(key) != null;
            }
        }

        private string BuildSubKey(string section)
        {
            return _rootSubKey + "\\" + section;
        }

        private static RegistryKey GetHive(RegistryRoot root)
        {
            switch (root)
            {
                case RegistryRoot.CurrentUser: return Registry.CurrentUser;
                case RegistryRoot.LocalMachine: return Registry.LocalMachine;
                case RegistryRoot.ClassesRoot: return Registry.ClassesRoot;
                case RegistryRoot.Users: return Registry.Users;
                case RegistryRoot.CurrentConfig: return Registry.CurrentConfig;
                default: throw new ArgumentOutOfRangeException(nameof(root));
            }
        }
    }
}
