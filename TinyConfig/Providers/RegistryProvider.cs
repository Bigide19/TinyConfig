using TinyConfig.Internal;
using Microsoft.Win32;
using System;
#if !NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

namespace TinyConfig.Providers
{
    // Registry 는 Windows 에서만 동작합니다. net5.0 이상에서는 이 표기가 있어야
    // 호출하는 쪽이 CA1416 경고로 플랫폼 제약을 알 수 있습니다. netstandard2.0 과
    // net4x 에는 이 특성이 없어서 조건부로 붙입니다.
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public class RegistryProvider : ITinyConfig
    {
        private readonly string _rootSubKey;
        private readonly RegistryKey _hive;

        public RegistryProvider(string subKey, RegistryRoot root = RegistryRoot.CurrentUser)
        {
            // .NET Framework 는 Windows 전용이라 확인할 필요가 없습니다. 그 밖의
            // 타깃에서 막지 않으면 생성은 조용히 성공하고, 나중에 Get 을 부를 때
            // 원인을 알 수 없는 NullReferenceException 이 납니다.
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException(
                    "The Registry provider requires Windows. Use FromFile, FromJson or FromXml instead.");
#endif

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

            if (string.IsNullOrWhiteSpace(valStr))
            {
                if (autoSave) Set(section, key, defaultValue);
                return defaultValue;
            }

            return ValueConverter.Convert(valStr, defaultValue);
        }

        public bool TryGet<T>(string section, string key, out T value)
        {
            return ValueConverter.TryConvert(Get(section, key, null, false), out value);
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
