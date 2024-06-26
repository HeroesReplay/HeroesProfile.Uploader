using System;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace HeroesProfile.Uploader.Core.OS.WinOS;

[SupportedOSPlatform("windows")]
public class WindowsStartupHelper(ILogger<WindowsStartupHelper> logger) : IStartupHelper
{
    private static string AppName => "Heroes Profile Uploader";
    private const string Name = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    
    // TODO: What is this with avalonia and no clickOnce
    private static string AppPath => Environment.GetFolderPath(Environment.SpecialFolder.Programs) + @"\Heroes Profile\Heroes Profile Uploader.appref-ms";

    public bool Add()
    {
        try {
            using (var rk = Registry.CurrentUser.OpenSubKey(Name, true)) {
                if (rk.GetValue(AppName) == null) {
                    rk.SetValue(AppName, AppPath);
                    return true;
                }
            }   
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to add to startup");
        }

        return false;
    }

    public bool IsStartupEnabled()
    {
        try {
            using (var rk = Registry.CurrentUser.OpenSubKey(Name, true)) {
                if (rk != null) {
                    return rk.GetValue(AppName) != null;
                }
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to check if startup is enabled");
        }

        return false;
    }

    public bool Remove()
    {
        try {
            using (var rk = Registry.CurrentUser.OpenSubKey(Name, true)) {
                if (rk != null) {
                    if (rk.GetValue(AppName) != null) {
                        rk.DeleteValue(AppName, false);
                        return true;
                    }
                }
            }
        }
        catch (Exception e) {
            logger.LogError(e, "Failed to remove from startup");
        }

        return false;
    }
}