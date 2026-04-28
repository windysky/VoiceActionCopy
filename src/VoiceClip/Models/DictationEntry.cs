namespace VoiceClip.Models;

/// <summary>
/// Represents a single dictation session entry.
/// </summary>
public class DictationEntry
{
    /// <summary>
    /// Unique identifier for this entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The full transcribed text from the dictation session.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// When this dictation was completed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Duration of the dictation session in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Truncated preview of the text (max 80 characters).
    /// </summary>
    public string Preview
    {
        get
        {
            if (string.IsNullOrEmpty(Text))
                return string.Empty;

            return Text.Length <= 80
                ? Text
                : Text[..80];
        }
    }
}
