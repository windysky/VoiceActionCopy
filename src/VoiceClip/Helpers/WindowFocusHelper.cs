using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VoiceClip.Helpers;

/// <summary>
/// Win32 wrappers for capturing the user's previous foreground window, switching focus back
/// to it, and synthesizing Ctrl+V to paste the dictation result.
///
/// The focus-switching path uses the AttachThreadInput trick to bypass Windows' foreground
/// lock (LockSetForegroundWindow / SPI_GETFOREGROUNDLOCKTIMEOUT). Without it, SetForegroundWindow
/// silently fails when our process didn't generate the most recent input event — which is
/// exactly the situation when dictation completes via silence timeout instead of a click.
/// </summary>
public static class WindowFocusHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo,
        [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const int SW_RESTORE = 9;

    public static IntPtr CaptureCurrentWindow() => GetForegroundWindow();

    public static bool BelongsToCurrentProcess(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == (uint)Process.GetCurrentProcess().Id;
    }

    /// <summary>
    /// Brings the specified window to the foreground and synthesizes Ctrl+V.
    /// Returns true if the window was successfully foregrounded and the input was sent;
    /// false if the target was invalid, gone, or could not be foregrounded.
    /// </summary>
    public static async Task<bool> PasteToWindowAsync(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle)) return false;

        // Restore from minimized so SetForegroundWindow has a visible window to bring up.
        if (IsIconic(windowHandle))
        {
            ShowWindow(windowHandle, SW_RESTORE);
        }

        if (!ForceForeground(windowHandle))
        {
            // Wait briefly and retry once — Windows sometimes refuses the first time and
            // accepts a second call after a tick.
            await Task.Delay(80).ConfigureAwait(false);
            if (!ForceForeground(windowHandle)) return false;
        }

        // Give the target window a moment to actually receive focus before we send keys.
        // 80ms was the previous value and was occasionally too short on busy systems.
        await Task.Delay(150).ConfigureAwait(false);

        // Final sanity check — if some other window grabbed focus in the meantime,
        // sending Ctrl+V would paste into the wrong place.
        if (GetForegroundWindow() != windowHandle)
        {
            return false;
        }

        SendCtrlV();
        return true;
    }

    /// <summary>
    /// Bypasses Windows' foreground lock by attaching our input queue to the target window's
    /// thread before calling SetForegroundWindow. Always detaches afterwards even on failure.
    /// </summary>
    private static bool ForceForeground(IntPtr hwnd)
    {
        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var targetThread = GetWindowThreadProcessId(hwnd, out _);
        var currentThread = GetCurrentThreadId();

        bool attachedToForeground = false;
        bool attachedToTarget = false;

        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attachedToForeground = AttachThreadInput(currentThread, foregroundThread, true);
            }
            if (targetThread != 0 && targetThread != currentThread && targetThread != foregroundThread)
            {
                attachedToTarget = AttachThreadInput(currentThread, targetThread, true);
            }

            BringWindowToTop(hwnd);
            return SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attachedToForeground)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
            if (attachedToTarget)
            {
                AttachThreadInput(currentThread, targetThread, false);
            }
        }
    }

    private static void SendCtrlV()
    {
        var inputs = new[]
        {
            MakeKey(VK_CONTROL, false),
            MakeKey(VK_V,       false),
            MakeKey(VK_V,       true),
            MakeKey(VK_CONTROL, true),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKey(ushort vk, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        data = new InputUnion
        {
            ki = new KEYBDINPUT { wVk = vk, dwFlags = keyUp ? KEYEVENTF_KEYUP : 0 }
        }
    };

    /// <summary>
    /// Injects text directly into the focused window as Unicode keystrokes,
    /// bypassing the clipboard. Works like Voice Access real-time dictation.
    /// </summary>
    public static void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var inputs = new INPUT[text.Length * 2];
        for (int i = 0; i < text.Length; i++)
        {
            inputs[i * 2] = new INPUT
            {
                type = INPUT_KEYBOARD,
                data = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = text[i],
                        dwFlags = KEYEVENTF_UNICODE
                    }
                }
            };
            inputs[i * 2 + 1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                data = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = text[i],
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                    }
                }
            };
        }
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
