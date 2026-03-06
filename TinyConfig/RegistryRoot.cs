namespace TinyConfig
{
    /// <summary>
    /// Specifies the Windows Registry root hive.
    /// </summary>
    public enum RegistryRoot
    {
        /// <summary>HKEY_CURRENT_USER</summary>
        CurrentUser,
        /// <summary>HKEY_LOCAL_MACHINE (requires admin privileges for write)</summary>
        LocalMachine,
        /// <summary>HKEY_CLASSES_ROOT</summary>
        ClassesRoot,
        /// <summary>HKEY_USERS</summary>
        Users,
        /// <summary>HKEY_CURRENT_CONFIG</summary>
        CurrentConfig
    }
}
