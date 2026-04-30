using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace VoiceClip.Helpers;

/// <summary>
/// Toast notification shown as a lightweight auto-closing popup near the tray.
/// </summary>
public class ToastNotification
{
    private readonly DispatcherTimer _closeTimer;
    private Border? _toast;
    private bool _isError;

    public ToastNotification(Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? notifyIcon)
    {
        _closeTimer = new DispatcherTimer();
        _closeTimer.Tick += OnClose;
    }

    public void Show(string message, string title = "VoiceClip")
    {
        _isError = false;
        ShowToast(message, TimeSpan.FromSeconds(1));
    }

    public void ShowError(string message, string title = "VoiceClip Error")
    {
        _isError = true;
        ShowToast(message, TimeSpan.FromSeconds(3));
    }

    private void ShowToast(string message, TimeSpan duration)
    {
        CloseCurrent();

        var workArea = SystemParameters.WorkArea;

        _toast = new Border
        {
            Background = _isError ? Brushes.OrangeRed : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 8, 14, 8),
            Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            }
        };

        var container = new Window
        {
            Width = 340,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true,
            Background = Brushes.Transparent,
            Content = _toast,
            Left = workArea.Right - 360,
            Top = workArea.Bottom - 80
        };

        container.Show();

        _closeTimer.Interval = duration;
        _closeTimer.Tag = container;
        _closeTimer.Start();
    }

    private void OnClose(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        CloseCurrent();
    }

    private void CloseCurrent()
    {
        if (_closeTimer.Tag is Window w)
        {
            _closeTimer.Tag = null;
            try { w.Close(); } catch { }
        }
    }
}
