using VoiceClip.Models;

namespace VoiceClip.Services;

/// <summary>
/// Interface for settings persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from disk, or returns defaults if no file exists.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    void Save(AppSettings settings);
}
