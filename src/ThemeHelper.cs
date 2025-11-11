using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CEMCP
{
    public static class ThemeHelper
    {
        public static bool IsInDarkMode()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsWindowsDarkMode();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsMacDarkMode();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IsLinuxDarkMode();
            }

            return false;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static bool IsWindowsDarkMode()
        {
            try
            {
                var value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 1);

                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMacDarkMode()
        {
            try
            {
                // Run 'defaults read -g AppleInterfaceStyle' in macOS
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "defaults",
                        Arguments = "read -g AppleInterfaceStyle",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim().Equals("Dark", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLinuxDarkMode()
        {
            try
            {
                // Common approach: check GTK or environment variable
                string gtkTheme = Environment.GetEnvironmentVariable("GTK_THEME") ?? "";
                if (!string.IsNullOrEmpty(gtkTheme) && gtkTheme.ToLower().Contains("dark"))
                    return true;

                // Could also check GNOME settings via gsettings
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gsettings",
                        Arguments = "get org.gnome.desktop.interface color-scheme",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim().ToLower();
                process.WaitForExit();
                return output.Contains("prefer-dark");
            }
            catch
            {
                return false;
            }
        }
    }
}