namespace VoiceClip.Tray;

/// <summary>
/// Manages the system tray icon with context menu and state transitions.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _notifyIcon;
    private bool _disposed;

    public event EventHandler? DictateClicked;
    public event EventHandler? HistoryClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? ExitClicked;

    public TrayIconManager()
    {
        try
        {
            _notifyIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon();

            _notifyIcon.ContextMenu = CreateContextMenu();
            _notifyIcon.ToolTipText = "VoiceClip - Ready";

            _notifyIcon.TrayMouseDoubleClick += (s, e) => HistoryClicked?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Non-critical - tray icon creation may fail in non-GUI contexts
        }
    }

    /// <summary>
    /// Sets the tray icon state.
    /// </summary>
    public void SetState(TrayState state, string? message = null)
    {
        if (_notifyIcon == null) return;

        _notifyIcon.ToolTipText = state switch
        {
            TrayState.Idle => "VoiceClip - Ready",
            TrayState.Recording => "VoiceClip - Recording...",
            TrayState.Error => $"VoiceClip - Error: {message ?? "Unknown"}",
            _ => "VoiceClip"
        };
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
