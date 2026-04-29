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
        _speechService.PartialResultReceived += OnPartialResult;
        _speechService.DictationCompleted += OnDictationCompleted;

        // Check speech availability
        CheckSpeechAvailabilityAsync();

        // Show startup notification
        _toastNotification?.Show("Ready — Ctrl+Alt+D to dictate, Ctrl+Alt+V for history", "VoiceClip");
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

        if (_speechService.IsRecording)
        {
            await _speechService.StopDictationAsync();
        }
        else
        {
            try
            {
                await _speechService.StartDictationAsync();
                _trayIconManager?.SetState(Tray.TrayState.Recording);
                _partialResultsIndicator = new PartialResultsIndicator();
                _partialResultsIndicator.Show();
            }
            catch (Exception ex)
            {
                LogError("ToggleDictationAsync failed", ex);
                _trayIconManager?.SetState(Tray.TrayState.Error, ex.Message);

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
            _partialResultsIndicator?.Close();
            _partialResultsIndicator = null;

            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                _historyService?.Add(e.Text, e.DurationSeconds);
                try
                {
                    _clipboardService?.SetText(e.Text);
                    _toastNotification?.Show("Copied to clipboard");
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    _toastNotification?.ShowError("Could not copy to clipboard — clipboard is busy");
                }
                catch (System.Security.SecurityException)
                {
                    _toastNotification?.ShowError("Could not copy to clipboard — access denied");
                }
            }
            else if (e.DurationSeconds > 3)
            {
                _toastNotification?.ShowError("No speech detected. Check microphone and privacy settings.");
            }
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
        _hotkeyService?.Dispose();
        _trayIconManager?.Dispose();
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

    private static void LogError(string context, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VoiceClip", "error.log");
            var message = $"[{DateTime.UtcNow:O}] {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, message);
        }
        catch { }
    }
}
