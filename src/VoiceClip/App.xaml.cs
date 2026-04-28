using System.IO;
using System.Windows;
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
    private const string MutexName = "VoiceClip_SingleInstance";
    private Mutex? _mutex;
    private MainWindow? _mainWindow;
    private Tray.TrayIconManager? _trayIconManager;
    private HotkeyService? _hotkeyService;
    private DateTime _recordingStartTime;
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
        base.OnStartup(e);

        // Single instance enforcement
        _mutex = new Mutex(true, MutexName, out bool createdNew);
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

        // Create hidden main window (for HWND and message pump)
        _mainWindow = new MainWindow();
        _mainWindow.Show();

        // Initialize tray icon
        _trayIconManager = new Tray.TrayIconManager();
        _trayIconManager.DictateClicked += OnDictateClicked;
        _trayIconManager.HistoryClicked += OnHistoryClicked;
        _trayIconManager.SettingsClicked += OnSettingsClicked;
        _trayIconManager.ExitClicked += OnExitClicked;

        // Initialize hotkeys
        _hotkeyService = new HotkeyService(_mainWindow.WindowHandle);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyService.RegisterDefaultHotkeys())
        {
            var toast = new ToastNotification(null);
            toast.ShowError("Failed to register hotkeys. Another application may be using them.");
        }

        // Initialize toast
        _toastNotification = new ToastNotification(null);

        // Wire speech events
        _speechService.PartialResultReceived += OnPartialResult;
        _speechService.DictationCompleted += OnDictationCompleted;

        // Check speech availability
        CheckSpeechAvailabilityAsync();
    }

    private async void CheckSpeechAvailabilityAsync()
    {
        if (_speechService == null) return;

        var available = await _speechService.IsAvailableAsync();
        if (!available)
        {
            _trayIconManager?.SetState(Tray.TrayState.Error, "Speech recognition not available");
            _toastNotification?.ShowError(
                "Speech recognition is not available. Enable it in Windows Settings.");
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
            var text = await _speechService.StopDictationAsync();
            _trayIconManager?.SetState(Tray.TrayState.Idle);
            _partialResultsIndicator?.Close();
            _partialResultsIndicator = null;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var duration = (DateTime.UtcNow - _recordingStartTime).TotalSeconds;
                var entry = _historyService?.Add(text, duration);
                _toastNotification?.Show("Copied to clipboard");
                if (entry != null && _clipboardService != null)
                {
                    _clipboardService.SetText(text);
                }
            }
        }
        else
        {
            try
            {
                _recordingStartTime = DateTime.UtcNow;
                await _speechService.StartDictationAsync();
                _trayIconManager?.SetState(Tray.TrayState.Recording);
                _partialResultsIndicator = new PartialResultsIndicator();
                _partialResultsIndicator.Show();
            }
            catch (Exception ex)
            {
                _trayIconManager?.SetState(Tray.TrayState.Error, ex.Message);
                _toastNotification?.ShowError("Failed to start speech recognition.");
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
        // Handled in ToggleDictationAsync
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
        _historyViewModel?.Dispose();
        _partialResultsIndicator?.Close();

        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        base.OnExit(e);
    }
}
