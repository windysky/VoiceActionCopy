namespace VoiceClip.Models;

/// <summary>
/// Application settings persisted to %APPDATA%\VoiceClip\settings.json.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Speech recognition language (default: en-US).
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Seconds of silence before auto-stopping dictation (default: 60, range: 10-300).
    /// </summary>
    public int SilenceTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of history entries to keep (default: 500, range: 50-5000).
    /// </summary>
    public int MaxHistoryEntries { get; set; } = 500;

    /// <summary>
    /// Whether to run VoiceClip on Windows startup.
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Hotkey combination for toggling dictation (default: Ctrl+Alt+D).
    /// </summary>
    public string DictateHotkey { get; set; } = "Ctrl+Alt+D";

    /// <summary>
    /// Hotkey combination for showing history popup (default: Ctrl+Alt+V).
    /// </summary>
    public string HistoryHotkey { get; set; } = "Ctrl+Alt+V";
}
