using System.Text;
using VoiceClip.Helpers;

namespace VoiceClip.Services;

/// <summary>
/// Continuous speech recognition using Windows.Media.SpeechRecognition.
/// Uses reflection-based async wrapper to avoid WinRT/SDK type conflicts.
/// </summary>
public class SpeechRecognitionService : ISpeechRecognitionService, IDisposable
{
    private bool _isRecording;
    private DateTime _recordingStartTime;
    private readonly string _language;
    private readonly int _silenceTimeoutSeconds;
    private readonly StringBuilder _recognizedText = new();

    // WinRT types (loaded via reflection to avoid compile-time conflicts)
    private object? _recognizer;

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
            await StopContinuousRecognitionAsync();
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
            // Try to create a SpeechRecognizer to check availability
            await InitializeRecognizerAsync();
            return true;
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

        // Use Windows.Media.SpeechRecognition via WinRT
        // The actual WinRT object creation happens at runtime
        var speechRecognizerType = Type.GetType(
            "Windows.Media.SpeechRecognition.SpeechRecognizer, Microsoft.Windows.SDK.NET");

        if (speechRecognizerType == null)
        {
            throw new InvalidOperationException(
                "Windows.Media.SpeechRecognition is not available. " +
                "Ensure Windows 11 with speech recognition enabled.");
        }

        _recognizer = Activator.CreateInstance(speechRecognizerType, _language);

        // Configure for dictation
        await ConfigureDictationAsync();
    }

    private async Task ConfigureDictationAsync()
    {
        if (_recognizer == null) return;

        var recognizerType = _recognizer.GetType();

        // Get ContinuousRecognitionSession
        var sessionProperty = recognizerType.GetProperty("ContinuousRecognitionSession");
        var session = sessionProperty?.GetValue(_recognizer);

        if (session != null)
        {
            // Set timeout
            var timeoutProperty = session.GetType().GetProperty("StopOnSilenceTimeout");
            if (timeoutProperty?.CanWrite == true)
            {
                timeoutProperty.SetValue(session, TimeSpan.FromSeconds(_silenceTimeoutSeconds));
            }
        }

        // Compile constraints
        var compileMethod = _recognizer.GetType().GetMethod("CompileConstraintsAsync");
        if (compileMethod != null)
        {
            var asyncAction = compileMethod.Invoke(_recognizer, null);
            if (asyncAction != null)
            {
                await WinRTAsyncHelper.AsTask(asyncAction);
            }
        }
    }

    private async Task StartContinuousRecognitionAsync()
    {
        if (_recognizer == null) return;

        var recognizerType = _recognizer.GetType();
        var sessionProperty = recognizerType.GetProperty("ContinuousRecognitionSession");
        var session = sessionProperty?.GetValue(_recognizer);

        if (session == null) return;

        // Wire up events
        WireUpResultGeneratedEvent(session);
        WireUpCompletedEvent(session);

        // Start async
        var startMethod = session.GetType().GetMethod("StartAsync");
        if (startMethod != null)
        {
            var asyncAction = startMethod.Invoke(session, null);
            if (asyncAction != null)
            {
                await WinRTAsyncHelper.AsTask(asyncAction);
            }
        }
    }

    private async Task StopContinuousRecognitionAsync()
    {
        if (_recognizer == null) return;

        var recognizerType = _recognizer.GetType();
        var sessionProperty = recognizerType.GetProperty("ContinuousRecognitionSession");
        var session = sessionProperty?.GetValue(_recognizer);

        if (session == null) return;

        var stopMethod = session.GetType().GetMethod("StopAsync");
        if (stopMethod != null)
        {
            var asyncAction = stopMethod.Invoke(session, null);
            if (asyncAction != null)
            {
                await WinRTAsyncHelper.AsTask(asyncAction);
            }
        }
    }

    private void WireUpResultGeneratedEvent(object session)
    {
        var eventInfo = session.GetType().GetEvent("ResultGenerated");
        if (eventInfo == null) return;

        var handlerType = eventInfo.EventHandlerType;
        if (handlerType == null) return;

        // Create delegate using reflection
        var methodInfo = GetType().GetMethod(nameof(OnResultGenerated),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (methodInfo == null) return;

        var handler = Delegate.CreateDelegate(handlerType, this, methodInfo);
        eventInfo.AddEventHandler(session, handler);
    }

    private void WireUpCompletedEvent(object session)
    {
        var eventInfo = session.GetType().GetEvent("Completed");
        if (eventInfo == null) return;

        var handlerType = eventInfo.EventHandlerType;
        if (handlerType == null) return;

        var methodInfo = GetType().GetMethod(nameof(OnSessionCompleted),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (methodInfo == null) return;

        var handler = Delegate.CreateDelegate(handlerType, this, methodInfo);
        eventInfo.AddEventHandler(session, handler);
    }

    private void OnResultGenerated(object? sender, dynamic e)
    {
        try
        {
            var result = e.Result;
            var text = (string)result.Text;
            AppendRecognizedText(text);
        }
        catch
        {
            // Ignore parsing errors in event handler
        }
    }

    private void OnSessionCompleted(object? sender, dynamic e)
    {
        // Session completed (e.g., silence timeout)
        _isRecording = false;
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            try
            {
                StopContinuousRecognitionAsync().Wait();
            }
            catch
            {
                // Best effort cleanup
            }
        }
        _recognizer = null;
    }
}
