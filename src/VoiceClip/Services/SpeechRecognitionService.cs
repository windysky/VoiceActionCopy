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
    private SpeechRecognizer? _recognizer;

    public bool IsRecording => _isRecording;

    public event EventHandler<PartialResultEventArgs>? PartialResultReceived;
    public event EventHandler<DictationResultEventArgs>? DictationCompleted;

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

        try
        {
            await InitializeRecognizerAsync();
            await StartContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            _isRecording = false;
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

        CleanupRecognizer();
        return text;
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await InitializeRecognizerAsync();
            return _recognizer != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Appends recognized text (called from WinRT event handlers).
    /// </summary>
    public void AppendRecognizedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var aggregatedText = AppendRecognizedSegment(text);
        PartialResultReceived?.Invoke(this, new PartialResultEventArgs { Text = aggregatedText });
    }

    private string AppendRecognizedSegment(string text)
    {
        var trimmedText = text.Trim();
        if (_recognizedText.Length > 0 &&
            !char.IsWhiteSpace(_recognizedText[^1]) &&
            !StartsWithPunctuation(trimmedText))
        {
            _recognizedText.Append(' ');
        }

        _recognizedText.Append(trimmedText);
        return _recognizedText.ToString();
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

            _recognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(_silenceTimeoutSeconds);
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

    private void OnSessionCompleted(SpeechContinuousRecognitionSession sender,
        SpeechContinuousRecognitionCompletedEventArgs args)
    {
        // If StopDictationAsync already handled completion, skip
        if (_stopRequested || !_isRecording) return;

        _isRecording = false;
        var text = _recognizedText.ToString();
        var duration = (DateTime.UtcNow - _recordingStartTime).TotalSeconds;

        DictationCompleted?.Invoke(this, new DictationResultEventArgs
        {
            Text = text,
            DurationSeconds = duration
        });

        CleanupRecognizer();
    }

    private void CleanupRecognizer()
    {
        if (_recognizer != null)
        {
            _recognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
            _recognizer.ContinuousRecognitionSession.Completed -= OnSessionCompleted;
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
        CleanupRecognizer();
    }
}
