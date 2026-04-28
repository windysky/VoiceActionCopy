using Microsoft.Win32;

namespace VoiceClip.Services;

/// <summary>
/// Manages running VoiceClip on Windows startup via registry.
/// </summary>
public class StartupService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "VoiceClip";

    /// <summary>
    /// Enables or disables running on Windows startup.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <param name="exePath">Path to the executable (required when enabling).</param>
    public bool SetStartup(bool enable, string? exePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return false;

            if (enable)
            {
                if (string.IsNullOrEmpty(exePath)) return false;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if VoiceClip is configured to run on startup.
    /// </summary>
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key == null) return false;

            var value = key.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
