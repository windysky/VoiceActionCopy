using System.Text;
using VoiceClip.Helpers;
using Windows.Media.SpeechRecognition;

namespace VoiceClip.Services;

/// <summary>
/// Continuous speech recognition using Windows.Media.SpeechRecognition.
/// </summary>
public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private volatile bool _isRecording;
    private bool _stopRequested;
    private DateTime _recordingStartTime;
    private readonly string _language;
    private readonly int _silenceTimeoutSeconds;
    private readonly StringBuilder _recognizedText = new();
    // Guards _recognizedText against concurrent access from
    // ContinuousRecognitionSession.ResultGenerated (background thread) and
    // SpeechRecognizer.HypothesisGenerated (background thread, possibly different one).
    private readonly object _textLock = new();
    private SpeechRecognizer? _recognizer;
    private string? _savedDefaultDeviceId;
    public string? PreferredDeviceId { get; set; }

    public bool IsRecording => _isRecording;

    public event EventHandler<PartialResultEventArgs>? PhraseCompleted;
    public event EventHandler<PartialResultEventArgs>? PartialResultReceived;
    public event EventHandler<DictationResultEventArgs>? DictationCompleted;
    public event EventHandler<string>? RecognitionError;

    public SpeechRecognitionService(string language = "en-US", int silenceTimeoutSeconds = 8)
    {
        _language = language;
        _silenceTimeoutSeconds = silenceTimeoutSeconds;
    }

    /// <inheritdoc/>
    public async Task StartDictationAsync()
    {
        if (_isRecording) return;

        CleanupRecognizer();
        _recognizedText.Clear();
        _recordingStartTime = DateTime.UtcNow;
        _stopRequested = false;
        _isRecording = true;

        if (!string.IsNullOrEmpty(PreferredDeviceId))
        {
            _savedDefaultDeviceId = AudioDeviceHelper.GetDefaultCommunicationDeviceId();
            AudioDeviceHelper.SetDefaultCommunicationDevice(PreferredDeviceId);
        }

        try
        {
            await InitializeRecognizerAsync();
            await StartContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            _isRecording = false;
            RestoreDefaultDevice();
            CleanupRecognizer();
            throw new InvalidOperationException("Failed to start speech recognition: " + ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> StopDictationAsync()
    {
        if (!_isRecording) return string.Empty;

        _stopRequested = true;
        _isRecording = false;
        var duration = (DateTime.UtcNow - _recordingStartTime).TotalSeconds;
        var text = _recognizedText.ToString();

        try
        {
            if (_recognizer?.ContinuousRecognitionSession != null)
            {
                await _recognizer.ContinuousRecognitionSession.StopAsync();
            }
        }
        catch
        {
            // Best effort stop
        }

        DictationCompleted?.Invoke(this, new DictationResultEventArgs
        {
            Text = text,
            DurationSeconds = duration
        });

        RestoreDefaultDevice();
        CleanupRecognizer();
        return text;
    }

    private void RestoreDefaultDevice()
    {
        if (_savedDefaultDeviceId != null)
        {
            AudioDeviceHelper.SetDefaultCommunicationDevice(_savedDefaultDeviceId);
            _savedDefaultDeviceId = null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await InitializeRecognizerAsync();
            var available = _recognizer != null;
            // Don't leave the recognizer alive between sessions
            CleanupRecognizer();
            return available;
        }
        catch
        {
            CleanupRecognizer();
            return false;
        }
    }

    /// <summary>
    /// Appends recognized text (called from WinRT event handlers).
    /// Fires PhraseCompleted with only the new incremental text (for real-time typing),
    /// then PartialResultReceived with the full accumulated text (for the popup display).
    /// </summary>
    public void AppendRecognizedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        int prevLength;
        lock (_textLock) { prevLength = _recognizedText.Length; }

        var aggregatedText = AppendRecognizedSegment(text);
        var incremental = aggregatedText[prevLength..];

        PhraseCompleted?.Invoke(this, new PartialResultEventArgs { Text = incremental });
        PartialResultReceived?.Invoke(this, new PartialResultEventArgs { Text = aggregatedText });
    }

    private string AppendRecognizedSegment(string text)
    {
        var trimmedText = text.Trim();
        lock (_textLock)
        {
            if (_recognizedText.Length > 0 &&
                !char.IsWhiteSpace(_recognizedText[^1]) &&
                !StartsWithPunctuation(trimmedText))
            {
                _recognizedText.Append(' ');
            }

            _recognizedText.Append(trimmedText);
            return _recognizedText.ToString();
        }
    }

    /// <summary>
    /// Builds a preview string composed of the finalized recognized text plus the in-progress
    /// hypothesis. Used to surface live word-by-word feedback while the speaker is still speaking.
    /// Does NOT mutate <see cref="_recognizedText"/> — hypotheses are speculative and get
    /// replaced by ResultGenerated when the phrase finalizes.
    /// </summary>
    private string BuildHypothesisPreview(string hypothesis)
    {
        var trimmed = hypothesis?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            lock (_textLock) { return _recognizedText.ToString(); }
        }

        lock (_textLock)
        {
            if (_recognizedText.Length == 0) return trimmed;
            var needsSpace = !char.IsWhiteSpace(_recognizedText[^1]) && !StartsWithPunctuation(trimmed);
            return needsSpace
                ? _recognizedText.ToString() + ' ' + trimmed
                : _recognizedText.ToString() + trimmed;
        }
    }

    private static bool StartsWithPunctuation(string text)
    {
        return !string.IsNullOrEmpty(text) && char.IsPunctuation(text[0]);
    }

    private async Task InitializeRecognizerAsync()
    {
        if (_recognizer != null)
        {
            return;
        }

        try
        {
            var language = new Windows.Globalization.Language(_language);
            _recognizer = new SpeechRecognizer(language);

            _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
            _recognizer.ContinuousRecognitionSession.Completed += OnSessionCompleted;
            // HypothesisGenerated fires word-by-word while the speaker is still mid-phrase.
            // Without this, PartialResultReceived only fires after a phrase finalizes (i.e.
            // after a brief silence), leaving the partial-results indicator empty during speech.
            _recognizer.HypothesisGenerated += OnHypothesisGenerated;

            // EndSilenceTimeout controls when each spoken phrase finalizes (ResultGenerated fires).
            // Leave at the OS default (~150ms) so phrase detection feels natural, matching
            // Windows Voice Access behavior. Setting it to a long value delays finalization.
            // InitialSilenceTimeout controls how long of silence before the session auto-stops.
            _recognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(_silenceTimeoutSeconds);

            _recognizer.Constraints.Add(
                new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation"));

            await _recognizer.CompileConstraintsAsync();
        }
        catch
        {
            _recognizer?.Dispose();
            _recognizer = null;
            throw;
        }
    }

    private async Task StartContinuousRecognitionAsync()
    {
        if (_recognizer == null) return;
        await _recognizer.ContinuousRecognitionSession.StartAsync();
    }

    private void OnResultGenerated(SpeechContinuousRecognitionSession sender,
        SpeechContinuousRecognitionResultGeneratedEventArgs args)
    {
        AppendRecognizedText(args.Result.Text);
    }

    private void OnHypothesisGenerated(SpeechRecognizer sender,
        SpeechRecognitionHypothesisGeneratedEventArgs args)
    {
        // Skip if a stop was requested between events to avoid surfacing stale text after the
        // session has been torn down on the UI side.
        if (_stopRequested || !_isRecording) return;

        var hypothesisText = args.Hypothesis?.Text;
        if (string.IsNullOrWhiteSpace(hypothesisText)) return;

        var preview = BuildHypothesisPreview(hypothesisText);
        PartialResultReceived?.Invoke(this, new PartialResultEventArgs { Text = preview });
    }

    private void OnSessionCompleted(SpeechContinuousRecognitionSession sender,
        SpeechContinuousRecognitionCompletedEventArgs args)
    {
        // If StopDictationAsync already handled completion, skip
        if (_stopRequested || !_isRecording) return;

        _isRecording = false;

        // Fire DictationCompleted on normal completion: Success, TimeoutExceeded, or
        // UserCanceled. UserCanceled fires when Windows grants mic access to another app
        // (notification, call app, etc.) — NOT a user action. Save whatever was captured
        // so the words aren't lost. Only surface RecognitionError for hardware failures.
        if (args.Status != SpeechRecognitionResultStatus.Success &&
            args.Status != SpeechRecognitionResultStatus.TimeoutExceeded &&
            args.Status != SpeechRecognitionResultStatus.UserCanceled)
        {
            RecognitionError?.Invoke(this, $"Speech recognition ended: {args.Status}");
            RestoreDefaultDevice();
            CleanupRecognizer();
            return;
        }

        var text = _recognizedText.ToString();
        var duration = (DateTime.UtcNow - _recordingStartTime).TotalSeconds;

        DictationCompleted?.Invoke(this, new DictationResultEventArgs
        {
            Text = text,
            DurationSeconds = duration
        });

        RestoreDefaultDevice();
        CleanupRecognizer();
    }

    private void CleanupRecognizer()
    {
        if (_recognizer != null)
        {
            _recognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
            _recognizer.ContinuousRecognitionSession.Completed -= OnSessionCompleted;
            _recognizer.HypothesisGenerated -= OnHypothesisGenerated;
            _recognizer.Dispose();
            _recognizer = null;
        }
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            try
            {
                var stopTask = _recognizer?.ContinuousRecognitionSession.StopAsync();
                if (stopTask != null)
                {
                    var task = WinRTAsyncHelper.AsTask(stopTask);
                    task.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch
            {
                // Best effort cleanup
            }
            _isRecording = false;
        }
        RestoreDefaultDevice();
        CleanupRecognizer();
    }
}
