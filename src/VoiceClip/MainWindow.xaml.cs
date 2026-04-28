using System.Windows;
using System.Windows.Interop;
using VoiceClip.Services;

namespace VoiceClip;

/// <summary>
/// Hidden main window. WPF requires at least one window for message pump.
/// Used to receive hotkey messages via HWND.
/// </summary>
public partial class MainWindow : Window
{
    private const int WM_HOTKEY = 0x0312;
    private HotkeyService? _hotkeyService;

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
            var helper = new WindowInteropHelper(this);
            return helper.Handle;
        }
    }

    /// <summary>
    /// Sets the hotkey service and hooks WndProc for WM_HOTKEY messages.
    /// Call after the window handle is created.
    /// </summary>
    public void InitializeHotkeyHook(HotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;

        var helper = new WindowInteropHelper(this);
        var handle = helper.Handle;

        if (handle != nint.Zero)
        {
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var hotkeyId = (int)wParam;
            _hotkeyService?.ProcessHotkeyMessage(hotkeyId);
            handled = true;
        }

        return nint.Zero;
    }
}
