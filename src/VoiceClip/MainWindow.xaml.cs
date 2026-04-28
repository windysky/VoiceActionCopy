using System.Windows;

namespace VoiceClip;

/// <summary>
/// Hidden main window. WPF requires at least one window for message pump.
/// Used to receive hotkey messages via HWND.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Exposes the window handle for hotkey registration.
    /// </summary>
    public nint WindowHandle
    {
        get
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            return helper.Handle;
        }
    }
}
