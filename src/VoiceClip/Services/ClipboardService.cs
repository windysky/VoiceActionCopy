using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace VoiceClip.Services;

/// <summary>
/// Clipboard operations using WPF Clipboard class.
///
/// Win32 clipboard is a single global resource that any process can hold open. When another
/// app (e.g. ClipboardManager, RDP, password manager) is reading or writing the clipboard,
/// our SetText/SetDataObject call fails with COMException 0x800401D0 (CLIPBRD_E_CANT_OPEN).
/// WPF's built-in retry is short and not always sufficient, so we layer our own retry loop on
/// top, and use SetDataObject(text, copy: true) instead of SetText so the data persists in
/// the clipboard after VoiceClip exits.
/// </summary>
public class ClipboardService : IClipboardService
{
    private const int MaxAttempts = 5;
    private const int DelayBetweenAttemptsMs = 60;

    /// <inheritdoc/>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Exception? lastException = null;
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                // copy: true → data is rendered into the clipboard immediately so it survives
                // after the source app (us) exits. This is what the user expects: dictate, then
                // close VoiceClip, and still be able to paste.
                Clipboard.SetDataObject(text, copy: true);
                return; // success
            }
            catch (COMException ex)
            {
                // 0x800401D0 CLIPBRD_E_CANT_OPEN — another process holds the clipboard.
                // Retry briefly; this is the primary cause of "clipboard sometimes misses".
                lastException = ex;
                Thread.Sleep(DelayBetweenAttemptsMs);
            }
            catch (ExternalException ex)
            {
                // Generic OLE failure — same retry strategy.
                lastException = ex;
                Thread.Sleep(DelayBetweenAttemptsMs);
            }
        }

        // All attempts failed. Caller's catch block in App.xaml.cs surfaces this as a toast.
        throw lastException ?? new InvalidOperationException("Clipboard could not be opened after retries.");
    }

    /// <inheritdoc/>
    public string? GetText()
    {
        // Read also tolerates transient failure — same retry policy.
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (COMException) when (attempt < MaxAttempts - 1)
            {
                Thread.Sleep(DelayBetweenAttemptsMs);
            }
            catch (ExternalException) when (attempt < MaxAttempts - 1)
            {
                Thread.Sleep(DelayBetweenAttemptsMs);
            }
        }
        return null;
    }
}
