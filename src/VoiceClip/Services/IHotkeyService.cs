namespace VoiceClip.Services;

/// <summary>
/// Interface for global hotkey registration.
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="id">Unique hotkey ID.</param>
    /// <param name="modifier">Modifier flags (ALT, CONTROL, SHIFT, WIN).</param>
    /// <param name="key">Virtual key code.</param>
    /// <returns>True if registration succeeded.</returns>
    bool RegisterHotKey(int id, uint modifier, uint key);

    /// <summary>
    /// Unregisters a global hotkey.
    /// </summary>
    bool UnregisterHotKey(int id);

    /// <summary>
    /// Event raised when a registered hotkey is pressed.
    /// </summary>
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;
}

/// <summary>
/// Event args for hotkey press events.
/// </summary>
public class HotkeyEventArgs : EventArgs
{
    public int Id { get; init; }
}
