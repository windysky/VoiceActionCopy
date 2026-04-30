using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using VoiceClip.Helpers;
using VoiceClip.Models;
using VoiceClip.Services;
using VoiceClip.ViewModels;
using VoiceClip.Views;

namespace VoiceClip;

/// <summary>
/// VoiceClip application entry point.
/// Handles single-instance enforcement, service wiring, and lifecycle management.
/// </summary>
public partial class App : Application
{
    private const string MutexName = "VoiceClip_SingleInstance_{B7E3F2A1-4D5C-6E8A-9F0B-1C2D3E4F5A6B}";
    private Mutex? _mutex;
    private bool _ownsMutex;
    private MainWindow? _mainWindow;
    private Tray.TrayIconManager? _trayIconManager;
    private HotkeyService? _hotkeyService;
    private SpeechRecognitionService? _speechService;
    private HistoryService? _historyService;
    private ClipboardService? _clipboardService;
    private SettingsService? _settingsService;
    private HistoryViewModel? _historyViewModel;
    private ToastNotification? _toastNotification;
    private PartialResultsIndicator? _partialResultsIndicator;
    private AppSettings? _settings;
    private FloatingButtonWindow? _floatingButton;
    private IntPtr _dictationTargetWindow;
    private System.Windows.Threading.DispatcherTimer? _windowTracker;
    private bool _isStartingDictation;
    private int _phrasesTyped;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogError("UnhandledException", ex ?? new Exception("Unknown"));
            MessageBox.Show($"Fatal error: {ex?.Message ?? "Unknown"}", "VoiceClip",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };
        DispatcherUnhandledException += (s, args) =>
        {
            LogError("DispatcherUnhandledException", args.Exception);
            MessageBox.Show($"UI error: {args.Exception.Message}", "VoiceClip",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LogError("UnobservedTaskException", args.Exception);
        };

        base.OnStartup(e);

        // Single instance enforcement
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;
        if (!createdNew)
        {
            MessageBox.Show("VoiceClip is already running.", "VoiceClip",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Initialize storage directory
        var storageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoiceClip");
        Directory.CreateDirectory(storageDir);

        // Initialize services
        _settingsService = new SettingsService(storageDir);
        _settings = _settingsService.Load();
        _historyService = new HistoryService(storageDir, _settings.MaxHistoryEntries);
        _clipboardService = new ClipboardService();
        _speechService = new SpeechRecognitionService(_settings.Language, _settings.SilenceTimeoutSeconds);

        // Initialize ViewModel
        _historyViewModel = new HistoryViewModel(_historyService, _clipboardService);
        _historyViewModel.EntryCopied += OnHistoryEntryCopied;
        _historyViewModel.EntryCopyFailed += OnHistoryEntryCopyFailed;

        // Create hidden main window (for HWND and message pump)
        _mainWindow = new MainWindow();
        // Force handle creation without showing the window
        var helper = new WindowInteropHelper(_mainWindow);
        helper.EnsureHandle();

        // Initialize tray icon
        _trayIconManager = new Tray.TrayIconManager();
        _trayIconManager.DictateClicked += OnDictateClicked;
        _trayIconManager.HistoryClicked += OnHistoryClicked;
        _trayIconManager.SettingsClicked += OnSettingsClicked;
        _trayIconManager.ExitClicked += OnExitClicked;

        // Initialize toast notifications with the actual TaskbarIcon
        _toastNotification = new ToastNotification(_trayIconManager.TaskbarIcon);

        // Initialize hotkeys and wire WM_HOTKEY message pump
        _hotkeyService = new HotkeyService(_mainWindow.WindowHandle);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _mainWindow.InitializeHotkeyHook(_hotkeyService);

        if (!_hotkeyService.RegisterDefaultHotkeys())
        {
            _toastNotification.ShowError("Failed to register hotkeys. Another application may be using them.");
        }

        // Wire speech events
        _speechService.PhraseCompleted += OnPhraseCompleted;
        _speechService.PartialResultReceived += OnPartialResult;
        _speechService.DictationCompleted += OnDictationCompleted;
        _speechService.RecognitionError += OnRecognitionError;

        // Create floating dictation button
        _floatingButton = new FloatingButtonWindow();
        _floatingButton.StartStopClicked += OnFloatingButtonClicked;
        _floatingButton.HistoryClicked += OnHistoryClicked;
        _floatingButton.SettingsClicked += OnSettingsClicked;
        _floatingButton.ExitClicked += OnExitClicked;
        _floatingButton.Show();

        // Track the last non-VoiceClip foreground window so we know where to paste
        _windowTracker = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _windowTracker.Tick += (_, _) =>
        {
            var hwnd = WindowFocusHelper.CaptureCurrentWindow();
            if (hwnd != IntPtr.Zero && !WindowFocusHelper.BelongsToCurrentProcess(hwnd))
                _dictationTargetWindow = hwnd;
        };
        _windowTracker.Start();

        // Floating button appearance is the startup signal — no balloon needed.
    }

    private async void CheckSpeechAvailabilityAsync()
    {
        if (_speechService == null) return;

        try
        {
            var available = await _speechService.IsAvailableAsync();
            if (!available)
            {
                _trayIconManager?.SetState(Tray.TrayState.Error, "Speech recognition not available");
                var result = MessageBox.Show(
                    "Speech recognition is not available.\n\nOpen Windows speech settings?",
                    "VoiceClip", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("ms-settings:privacy-speech") { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            LogError("CheckSpeechAvailabilityAsync", ex);
            _trayIconManager?.SetState(Tray.TrayState.Error, ex.Message);
            _toastNotification?.ShowError($"Speech check failed: {ex.Message}");
        }
    }

    private async void OnDictateClicked(object? sender, EventArgs e)
    {
        await ToggleDictationAsync();
    }

    private async void OnFloatingButtonClicked(object? sender, EventArgs e)
    {
        await ToggleDictationAsync();
    }

    private async void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        switch (e.Id)
        {
            case HotkeyService.HOTKEY_DICTATE:
                await ToggleDictationAsync();
                break;
            case HotkeyService.HOTKEY_HISTORY:
                ShowHistoryPopup();
                break;
        }
    }

    private async System.Threading.Tasks.Task ToggleDictationAsync()
    {
        if (_speechService == null) return;

        // Guard must be at top: a second click that arrives while StartDictationAsync is
        // awaited sees IsRecording=true and would call StopDictationAsync, killing the
        // session before it starts. Block ALL re-entrant calls during the start sequence.
        if (_isStartingDictation) return;

        if (_speechService.IsRecording)
        {
            await _speechService.StopDictationAsync();
        }
        else
        {
            _isStartingDictation = true;
            try
            {
                // Capture the user's current foreground window NOW, in addition to whatever the
                // 200ms background tracker last saw. This handles two cases the tracker misses:
                //   1. User just launched VoiceClip and pressed dictate before ever clicking
                //      another window — _dictationTargetWindow would be IntPtr.Zero.
                //   2. User changed apps within the last 200ms and the tracker hasn't ticked yet.
                // The floating button uses WS_EX_NOACTIVATE so a click on it does NOT change
                // the foreground, which means the foreground here is still the user's intended
                // paste target. For hotkey trigger, the foreground is the user's current app.
                var currentForeground = WindowFocusHelper.CaptureCurrentWindow();
                if (currentForeground != IntPtr.Zero &&
                    !WindowFocusHelper.BelongsToCurrentProcess(currentForeground))
                {
                    _dictationTargetWindow = currentForeground;
                }

                // Set UI to recording state optimistically before the await so there is no
                // window where IsRecording=true but the button still shows idle.
                _phrasesTyped = 0;
                _trayIconManager?.SetState(Tray.TrayState.Recording);
                _floatingButton?.SetRecording(true);
                _partialResultsIndicator = new PartialResultsIndicator();
                _partialResultsIndicator.Show();

                await _speechService.StartDictationAsync();
            }
            catch (Exception ex)
            {
                LogError("ToggleDictationAsync failed", ex);
                _trayIconManager?.SetState(Tray.TrayState.Error, ex.Message);
                _floatingButton?.SetRecording(false);
                _partialResultsIndicator?.Close();
                _partialResultsIndicator = null;

                if (ex.Message.Contains("privacy", StringComparison.OrdinalIgnoreCase))
                {
                    var result = MessageBox.Show(
                        "Speech recognition requires you to accept the Windows speech privacy policy.\n\n" +
                        "Open Settings → Privacy & security → Speech now?",
                        "VoiceClip", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo("ms-settings:privacy-speech") { UseShellExecute = true });
                    }
                }
                else
                {
                    _toastNotification?.ShowError($"Failed to start speech: {ex.Message}");
                }
            }
            finally
            {
                _isStartingDictation = false;
            }
        }
    }

    private void OnPhraseCompleted(object? sender, PartialResultEventArgs e)
    {
        // Type each phrase directly into the focused window as it is recognized —
        // no clipboard involvement, no focus switch needed, identical to Voice Access.
        if (!string.IsNullOrEmpty(e.Text))
        {
            WindowFocusHelper.TypeText(e.Text);
            _phrasesTyped++;
        }
    }

    private void OnPartialResult(object? sender, PartialResultEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _partialResultsIndicator?.UpdatePartialText(e.Text);
        });
    }

    private void OnDictationCompleted(object? sender, DictationResultEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _trayIconManager?.SetState(Tray.TrayState.Idle);
            _floatingButton?.SetRecording(false);
            _partialResultsIndicator?.Close();
            _partialResultsIndicator = null;

            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                _historyService?.Add(e.Text, e.DurationSeconds);
                try
                {
                    // Always put the full text on the clipboard so the user can paste it
                    // elsewhere with Ctrl+V even after the session ends.
                    _clipboardService?.SetText(e.Text);

                    if (_phrasesTyped > 0)
                    {
                        // Text was already typed directly into the target window phrase-by-phrase.
                        // No need to paste again — just confirm with a toast.
                        _toastNotification?.Show("Dictated");
                    }
                    else
                    {
                        // No phrases were typed in real-time (e.g., very quick session or
                        // StopDictationAsync called before any ResultGenerated). Fall back to
                        // the clipboard+paste approach.
                        var target = _dictationTargetWindow;
                        _ = WindowFocusHelper.PasteToWindowAsync(target)
                            .ContinueWith(t => Dispatcher.Invoke(() =>
                            {
                                var pasted = t.Status == TaskStatus.RanToCompletion && t.Result;
                                if (t.IsFaulted)
                                {
                                    LogError("PasteToWindowAsync", t.Exception?.GetBaseException()
                                        ?? new Exception("Paste task faulted"));
                                }
                                _toastNotification?.Show(pasted
                                    ? "Copied and pasted"
                                    : "Copied to clipboard");
                            }));
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    _toastNotification?.ShowError("Could not copy to clipboard — clipboard is busy");
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    _toastNotification?.ShowError("Could not copy to clipboard — clipboard error");
                }
                catch (System.Security.SecurityException)
                {
                    _toastNotification?.ShowError("Could not copy to clipboard — access denied");
                }
            }
            else if (e.DurationSeconds > 3)
            {
                _toastNotification?.Show("No speech detected");
            }
        });
    }

    private void OnRecognitionError(object? sender, string message)
    {
        LogError("RecognitionError", new Exception(message));
        Dispatcher.Invoke(() =>
        {
            _trayIconManager?.SetState(Tray.TrayState.Idle);
            _floatingButton?.SetRecording(false);
            _partialResultsIndicator?.Close();
            _partialResultsIndicator = null;

            // Map raw WinRT status strings to user-actionable guidance
            var userMessage = message switch
            {
                var m when m.Contains("MicrophoneUnavailable") =>
                    "Microphone unavailable. Check Settings → Privacy → Microphone.",
                var m when m.Contains("AudioQualityFailure") =>
                    "Microphone audio quality too low. Check your microphone connection.",
                _ => message
            };
            _toastNotification?.ShowError(userMessage);
        });
    }

    private void ShowHistoryPopup()
    {
        Dispatcher.Invoke(() =>
        {
            if (_historyViewModel == null) return;
            _historyViewModel.RefreshEntries();
            var popup = new HistoryPopup(_historyViewModel);
            popup.Show();
            popup.Activate();
        });
    }

    private void OnHistoryClicked(object? sender, EventArgs e)
    {
        ShowHistoryPopup();
    }

    private void OnHistoryEntryCopied(object? sender, EventArgs e)
    {
        _toastNotification?.Show("Copied to clipboard");
    }

    private void OnHistoryEntryCopyFailed(object? sender, EntryCopyFailedEventArgs e)
    {
        _toastNotification?.ShowError(e.Message);
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_settings == null || _settingsService == null) return;
            var settingsWindow = new SettingsWindow(_settingsService, _settings);
            settingsWindow.Owner = _mainWindow;
            settingsWindow.ShowDialog();
        });
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _windowTracker?.Stop();
        _floatingButton?.Close();
        _hotkeyService?.Dispose();
        _trayIconManager?.Dispose();
        if (_speechService != null)
        {
            _speechService.RecognitionError -= OnRecognitionError;
        }
        _speechService?.Dispose();
        if (_historyViewModel != null)
        {
            _historyViewModel.EntryCopied -= OnHistoryEntryCopied;
            _historyViewModel.EntryCopyFailed -= OnHistoryEntryCopyFailed;
        }
        _historyViewModel?.Dispose();
        _partialResultsIndicator?.Close();

        if (_mutex != null)
        {
            if (_ownsMutex)
            {
                _mutex.ReleaseMutex();
            }
            _mutex.Dispose();
        }

        base.OnExit(e);
    }

    private static readonly long MaxErrorLogSize = 5 * 1024 * 1024; // 5 MB

    private static void LogError(string context, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VoiceClip", "error.log");
            var message = $"[{DateTime.UtcNow:O}] {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";

            if (File.Exists(logPath))
            {
                try
                {
                    var size = new FileInfo(logPath).Length;
                    if (size > MaxErrorLogSize)
                        File.WriteAllText(logPath, message);
                    else
                        File.AppendAllText(logPath, message);
                }
                catch
                {
                    File.AppendAllText(logPath, message);
                }
            }
            else
            {
                File.AppendAllText(logPath, message);
            }
        }
        catch { }
    }
}
