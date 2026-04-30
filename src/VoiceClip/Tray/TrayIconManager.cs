using System.Drawing;
using System.IO;
using System.Windows;

namespace VoiceClip.Tray;

/// <summary>
/// Manages the system tray icon with context menu and state transitions.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _notifyIcon;
    private bool _disposed;
    private System.Windows.Threading.DispatcherTimer? _clickTimer;
    private bool _clickWaiting;

    public event EventHandler? DictateClicked;
    public event EventHandler? HistoryClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? ExitClicked;

    public TrayIconManager()
    {
        try
        {
            _notifyIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();

            var icon = LoadIcon();
            _notifyIcon.Icon = icon ?? SystemIcons.Information;

            _notifyIcon.ToolTipText = "VoiceClip - Ready";

            _clickTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _clickTimer.Tick += OnClickTimerTick;

            _notifyIcon.TrayLeftMouseUp += OnTrayLeftMouseUp;

            _notifyIcon.TrayRightMouseUp += (s, e) =>
            {
                var menu = CreateContextMenu();
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                menu.IsOpen = true;
            };
        }
        catch
        {
            // Non-critical - tray icon creation may fail in non-GUI contexts
        }
    }

    private void OnTrayLeftMouseUp(object? s, EventArgs e)
    {
        if (_clickWaiting)
        {
            // Second click within the double-click window → double-click
            _clickTimer.Stop();
            _clickWaiting = false;
            DictateClicked?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // First click — start timer, fire HistoryClicked if no second click arrives
            _clickWaiting = true;
            _clickTimer.Start();
        }
    }

    private void OnClickTimerTick(object? sender, EventArgs e)
    {
        _clickTimer.Stop();
        _clickWaiting = false;
        HistoryClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the TaskbarIcon for use by toast notifications.
    /// </summary>
    public Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? TaskbarIcon => _notifyIcon;

    /// <summary>
    /// Sets the tray icon state.
    /// </summary>
    public void SetState(TrayState state, string? message = null)
    {
        if (_disposed || _notifyIcon == null) return;

        // Try to load state-specific icon
        var icon = LoadIconForState(state);
        if (icon != null)
        {
            _notifyIcon.Icon = icon;
        }
        else
        {
            // Use color-coded system icons as fallback
            _notifyIcon.Icon = state switch
            {
                TrayState.Recording => SystemIcons.Exclamation,
                TrayState.Error => SystemIcons.Error,
                _ => SystemIcons.Information
            };
        }

        _notifyIcon.ToolTipText = state switch
        {
            TrayState.Idle => "VoiceClip - Ready",
            TrayState.Recording => "VoiceClip - Recording...",
            TrayState.Error => $"VoiceClip - Error: {message ?? "Unknown"}",
            _ => "VoiceClip"
        };
    }

    private Icon? LoadIcon()
    {
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = Path.Combine(exeDir, "Assets", "mic-idle.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch { }
        return null;
    }

    private Icon? LoadIconForState(TrayState state)
    {
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var filename = state switch
            {
                TrayState.Idle => "mic-idle.ico",
                TrayState.Recording => "mic-recording.ico",
                TrayState.Error => "mic-error.ico",
                _ => "mic-idle.ico"
            };

            var iconPath = Path.Combine(exeDir, "Assets", filename);
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch { }
        return null;
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var dictateItem = new System.Windows.Controls.MenuItem
        {
            Header = "Dictate (Ctrl+Alt+D)"
        };
        dictateItem.Click += (s, e) => DictateClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(dictateItem);

        var historyItem = new System.Windows.Controls.MenuItem
        {
            Header = "History (Ctrl+Alt+V)"
        };
        historyItem.Click += (s, e) => HistoryClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(historyItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settingsItem = new System.Windows.Controls.MenuItem
        {
            Header = "Settings"
        };
        settingsItem.Click += (s, e) => SettingsClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem
        {
            Header = "Exit"
        };
        exitItem.Click += (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _notifyIcon?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Tray icon state enumeration.
/// </summary>
public enum TrayState
{
    Idle,
    Recording,
    Error
}
