using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ViperaSecurity.Services
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "ViperaSecurity";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                var value = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }

        public static bool SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return false;

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName 
                        ?? Environment.ProcessPath 
                        ?? string.Empty;

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        return true;
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    return true;
                }
            }
            catch
            {
                // Fallback
            }
            return false;
        }
    }
}
