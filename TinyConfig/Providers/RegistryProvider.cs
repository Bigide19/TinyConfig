using TinyConfig.Internal;
using Microsoft.Win32;
using System;

namespace TinyConfig.Providers
{
    public class RegistryProvider : ITinyConfig
    {
        private readonly string _rootSubKey;
        private readonly RegistryKey _hive;

        public RegistryProvider(string subKey, RegistryRoot root = RegistryRoot.CurrentUser)
        {
            _rootSubKey = subKey;
            _hive = GetHive(root);
        }

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

        public T Get<T>(string section, string key, T defaultValue = default(T), bool autoSave = false)
        {
            string valStr = Get(section, key, null, false);

            if (valStr == null)
            {
                if (autoSave) Set(section, key, defaultValue);
                return defaultValue;
            }

            return ValueConverter.Convert(valStr, defaultValue);
        }

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
