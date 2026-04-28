namespace VoiceClip.Services;

/// <summary>
/// Interface for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies text to the Windows clipboard.
    /// </summary>
    void SetText(string text);

    /// <summary>
    /// Gets the current text from the clipboard, or null if no text is available.
    /// </summary>
    string? GetText();
}
