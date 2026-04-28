using System.Runtime.InteropServices;

namespace VoiceClip.Services;

/// <summary>
/// Global hotkey registration using P/Invoke RegisterHotKey/UnregisterHotKey.
/// Requires a window handle (HWND) to receive WM_HOTKEY messages.
/// </summary>
public class HotkeyService : IHotkeyService, IDisposable
{
    private readonly nint _windowHandle;
    private readonly HashSet<int> _registeredIds = [];
    private bool _disposed;

    // P/Invoke declarations
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    // Modifier constants
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    // Virtual key codes
    public const uint VK_D = 0x44;
    public const uint VK_V = 0x56;

    // Hotkey IDs
    public const int HOTKEY_DICTATE = 1;
    public const int HOTKEY_HISTORY = 2;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public HotkeyService(nint windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <inheritdoc/>
    public bool RegisterHotKey(int id, uint modifier, uint key)
    {
        if (_registeredIds.Contains(id))
        {
            UnregisterHotKey(id);
        }

        var result = RegisterHotKey(_windowHandle, id, modifier, key);
        if (result)
        {
            _registeredIds.Add(id);
        }
        return result;
    }

    /// <inheritdoc/>
    public bool UnregisterHotKey(int id)
    {
        var result = UnregisterHotKey(_windowHandle, id);
        if (result)
        {
            _registeredIds.Remove(id);
        }
        return result;
    }

    /// <summary>
    /// Processes a WM_HOTKEY message. Called from the window's WndProc.
    /// </summary>
    public void ProcessHotkeyMessage(int hotkeyId)
    {
        HotkeyPressed?.Invoke(this, new HotkeyEventArgs { Id = hotkeyId });
    }

    /// <summary>
    /// Registers the default hotkeys (Ctrl+Alt+D for dictate, Ctrl+Alt+V for history).
    /// </summary>
    /// <returns>True if all hotkeys registered successfully.</returns>
    public bool RegisterDefaultHotkeys()
    {
        var dictateRegistered = RegisterHotKey(HOTKEY_DICTATE, MOD_CONTROL | MOD_ALT, VK_D);
        var historyRegistered = RegisterHotKey(HOTKEY_HISTORY, MOD_CONTROL | MOD_ALT, VK_V);
        return dictateRegistered && historyRegistered;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var id in _registeredIds.ToList())
            {
                UnregisterHotKey(id);
            }
            _registeredIds.Clear();
            _disposed = true;
        }
    }
}
