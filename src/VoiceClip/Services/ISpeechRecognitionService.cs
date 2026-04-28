namespace VoiceClip.Services;

/// <summary>
/// Interface for speech recognition operations.
/// </summary>
public interface ISpeechRecognitionService
{
    /// <summary>
    /// Whether dictation is currently active.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Starts continuous dictation.
    /// </summary>
    Task StartDictationAsync();

    /// <summary>
    /// Stops dictation and returns the final recognized text.
    /// </summary>
    Task<string> StopDictationAsync();

    /// <summary>
    /// Checks if speech recognition is available on this system.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Event raised when partial recognition results are available.
    /// </summary>
    event EventHandler<PartialResultEventArgs>? PartialResultReceived;

    /// <summary>
    /// Event raised when dictation completes with final text.
    /// </summary>
    event EventHandler<DictationResultEventArgs>? DictationCompleted;
}

/// <summary>
/// Event args for partial speech recognition results.
/// </summary>
public class PartialResultEventArgs : EventArgs
{
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// Event args for completed dictation results.
/// </summary>
public class DictationResultEventArgs : EventArgs
{
    public string Text { get; init; } = string.Empty;
    public double DurationSeconds { get; init; }
}
