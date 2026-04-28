namespace VoiceClip.Helpers;

/// <summary>
/// Simple toast notification system using the tray icon balloon tip.
/// </summary>
public class ToastNotification
{
    private readonly Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _notifyIcon;

    public ToastNotification(Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? notifyIcon)
    {
        _notifyIcon = notifyIcon;
    }

    /// <summary>
    /// Shows a toast notification with the specified message.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="title">Optional title (defaults to "VoiceClip").</param>
    /// <param name="durationMs">Duration in milliseconds (defaults to 2000).</param>
    public void Show(string message, string title = "VoiceClip", int durationMs = 2000)
    {
        if (_notifyIcon == null) return;

        try
        {
            _notifyIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
        catch
        {
            // Non-critical - toast may fail in non-GUI contexts
        }
    }

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    public void ShowError(string message, string title = "VoiceClip Error")
    {
        if (_notifyIcon == null) return;

        try
        {
            _notifyIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
        }
        catch
        {
            // Non-critical
        }
    }
}
