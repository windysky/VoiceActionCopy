using System.Windows;

namespace VoiceClip.Services;

/// <summary>
/// Clipboard operations using WPF Clipboard class.
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <inheritdoc/>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Clipboard.SetText(text);
    }

    /// <inheritdoc/>
    public string? GetText()
    {
        return Clipboard.ContainsText() ? Clipboard.GetText() : null;
    }
}
