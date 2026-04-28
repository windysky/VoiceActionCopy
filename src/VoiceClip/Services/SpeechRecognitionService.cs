using System.Text;
using Windows.Media.SpeechRecognition;

namespace VoiceClip.Services;

/// <summary>
/// Continuous speech recognition using Windows.Media.SpeechRecognition.
/// </summary>
public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private bool _isRecording;
    private DateTime _recordingStartTime;
    private readonly string _language;
    private readonly int _silenceTimeoutSeconds;
    private readonly StringBuilder _recognizedText = new();
    private SpeechRecognizer? _recognizer;

    public bool IsRecording => _isRecording;

    public event EventHandler<PartialResultEventArgs>? PartialResultReceived;
    public event EventHandler<DictationResultEventArgs>? DictationCompleted;

    public SpeechRecognitionService(string language = "en-US", int silenceTimeoutSeconds = 60)
    {
        _language = language;
        _silenceTimeoutSeconds = silenceTimeoutSeconds;
    }

    /// <inheritdoc/>
    public async Task StartDictationAsync()
    {
        if (_isRecording) return;

        _recognizedText.Clear();
        _recordingStartTime = DateTime.UtcNow;
        _isRecording = true;

        try
        {
            await InitializeRecognizerAsync();
            await StartContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            _isRecording = false;
            throw new InvalidOperationException("Failed to start speech recognition: " + ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> StopDictationAsync()
    {
        if (!_isRecording) return string.Empty;

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
        if (string.IsNullOrEmpty(text)) return;

        _recognizedText.Append(text);
        PartialResultReceived?.Invoke(this, new PartialResultEventArgs { Text = text });
    }

    private async Task InitializeRecognizerAsync()
    {
        if (_recognizer != null) return;

        var language = new Windows.Globalization.Language(_language);
        _recognizer = new SpeechRecognizer(language);

        _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
        _recognizer.ContinuousRecognitionSession.Completed += OnSessionCompleted;

        _recognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(_silenceTimeoutSeconds);

        await _recognizer.CompileConstraintsAsync();
    }

    private async Task StartContinuousRecognitionAsync()
    {
        if (_recognizer == null) return;
        await _recognizer.ContinuousRecognitionSession.StartAsync();
    }

    private void OnResultGenerated(SpeechContinuousRecognitionSession sender,
        SpeechContinuousRecognitionResultGeneratedEventArgs args)
    {
        var text = args.Result.Text;
        AppendRecognizedText(text);
    }

    private void OnSessionCompleted(SpeechContinuousRecognitionSession sender,
        SpeechContinuousRecognitionCompletedEventArgs args)
    {
        _isRecording = false;
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            try
            {
                _recognizer?.ContinuousRecognitionSession.StopAsync().AsTask().Wait();
            }
            catch
            {
                // Best effort cleanup
            }
        }
        _recognizer?.Dispose();
        _recognizer = null;
    }
}
