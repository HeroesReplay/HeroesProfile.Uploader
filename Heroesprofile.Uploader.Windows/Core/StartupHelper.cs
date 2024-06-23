using Microsoft.Win32;

using System;

namespace Heroesprofile.Uploader.Windows.Core
{
    public static class StartupHelper
    {
        private const string Name = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static string AppName => "Heroes Profile Uploader";
        private static string AppPath => Environment.GetFolderPath(Environment.SpecialFolder.Programs) + @"\Heroes Profile\Heroes Profile Uploader.appref-ms";
        private static RegistryKey RegistryKey => Registry.CurrentUser.OpenSubKey(Name, true);

        public static void Add()
        { 
            using (var rk = RegistryKey)
            {
                if (rk.GetValue(AppName) == null) {
                    rk.SetValue("Heroes Profile Uploader", AppPath);
                }
            }
        }

        public static bool IsStartupEnabled()
        {
            using (var rk = RegistryKey)
            {
                var value = rk.GetValue(AppName);
                if (value is null) return false;
                if (value is not null) return true;
            }

            return false;
        }

        public static void Remove()
        {
            using(var rk = RegistryKey)
            {
                if (rk.GetValue(AppName) != null) {
                    rk.DeleteValue(AppName, false);
                }
            }
        }
    }
}