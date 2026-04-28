using System.ComponentModel;

namespace VoiceClip.Models;

/// <summary>
/// Application settings persisted to %APPDATA%\VoiceClip\settings.json.
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private string _language = "en-US";
    private int _silenceTimeoutSeconds = 60;
    private int _maxHistoryEntries = 500;
    private bool _runOnStartup;
    private string _dictateHotkey = "Ctrl+Alt+D";
    private string _historyHotkey = "Ctrl+Alt+V";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set { _language = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language))); }
    }

    public int SilenceTimeoutSeconds
    {
        get => _silenceTimeoutSeconds;
        set { _silenceTimeoutSeconds = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SilenceTimeoutSeconds))); }
    }

    public int MaxHistoryEntries
    {
        get => _maxHistoryEntries;
        set { _maxHistoryEntries = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxHistoryEntries))); }
    }

    public bool RunOnStartup
    {
        get => _runOnStartup;
        set { _runOnStartup = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunOnStartup))); }
    }

    public string DictateHotkey
    {
        get => _dictateHotkey;
        set { _dictateHotkey = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DictateHotkey))); }
    }

    public string HistoryHotkey
    {
        get => _historyHotkey;
        set { _historyHotkey = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryHotkey))); }
    }
}
