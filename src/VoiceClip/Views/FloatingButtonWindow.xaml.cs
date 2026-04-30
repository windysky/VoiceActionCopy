using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VoiceClip.Views;

public partial class FloatingButtonWindow : Window
{
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private Point _dragStart;
    private bool _isDragging;
    private const double DragThreshold = 5.0;

    public event EventHandler? StartStopClicked;
    public event EventHandler? HistoryClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? ExitClicked;

    public FloatingButtonWindow()
    {
        InitializeComponent();
        PositionInitial();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Prevent this window from stealing focus when clicked
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
    }

    public void SetRecording(bool isRecording)
    {
        if (isRecording)
        {
            ButtonFill.Color = Color.FromRgb(0xC4, 0x2B, 0x1C);
            MicIcon.Text = ""; // StopSolid (Segoe MDL2 Assets U+E71C)
            StartPulse();
        }
        else
        {
            ButtonFill.Color = Color.FromRgb(0x00, 0x78, 0xD4);
            MicIcon.Text = ""; // Microphone (Segoe MDL2 Assets U+E720)
            StopPulse();
        }
    }

    private void StartPulse()
    {
        var anim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.6,
            Duration = new Duration(TimeSpan.FromSeconds(0.65)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        ButtonCircle.BeginAnimation(OpacityProperty, anim);
    }

    private void StopPulse()
    {
        ButtonCircle.BeginAnimation(OpacityProperty, null);
        ButtonCircle.Opacity = 1.0;
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var dictateItem = new System.Windows.Controls.MenuItem { Header = "Dictate (Ctrl+Alt+D)" };
        dictateItem.Click += (s, _) => StartStopClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(dictateItem);

        var historyItem = new System.Windows.Controls.MenuItem { Header = "History (Ctrl+Alt+V)" };
        historyItem.Click += (s, _) => HistoryClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(historyItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (s, _) => SettingsClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, _) => ExitClicked?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void PositionInitial()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 120;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = PointToScreen(e.GetPosition(this));
        _isDragging = false;
        CaptureMouse();
        e.Handled = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var current = PointToScreen(e.GetPosition(this));
        var dx = current.X - _dragStart.X;
        var dy = current.Y - _dragStart.Y;

        if (!_isDragging && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold))
            _isDragging = true;

        if (_isDragging)
        {
            Left += dx;
            Top += dy;
            _dragStart = current;
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        if (!_isDragging)
            StartStopClicked?.Invoke(this, EventArgs.Empty);
        _isDragging = false;
        e.Handled = true;
    }
}
