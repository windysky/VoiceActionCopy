using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using VoiceClip.Models;
using VoiceClip.Services;

namespace VoiceClip.ViewModels;

/// <summary>
/// ViewModel for the history popup. Manages dictation entries,
/// search filtering, and clipboard copy commands.
/// </summary>
public class HistoryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IHistoryService _historyService;
    private readonly IClipboardService _clipboardService;
    private string _searchQuery = string.Empty;
    private ObservableCollection<DictationEntry> _entries;
    private DictationEntry? _selectedEntry;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? EntryCopied;

    public HistoryViewModel(IHistoryService historyService, IClipboardService clipboardService)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
        _entries = new ObservableCollection<DictationEntry>(_historyService.GetAll());

        CopyCommand = new RelayCommand<DictationEntry>(OnCopyEntry);
        DeleteCommand = new RelayCommand<DictationEntry>(OnDeleteEntry);
        ClearAllCommand = new RelayCommand(OnClearAll);
    }

    /// <summary>
    /// Filtered dictation entries displayed in the popup.
    /// </summary>
    public ObservableCollection<DictationEntry> Entries => _entries;

    /// <summary>
    /// The currently selected entry.
    /// </summary>
    public DictationEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEntry)));
        }
    }

    /// <summary>
    /// Search query for filtering entries.
    /// </summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchQuery)));
                RefreshEntries();
            }
        }
    }

    /// <summary>
    /// Command to copy an entry's text to clipboard.
    /// </summary>
    public ICommand CopyCommand { get; }

    /// <summary>
    /// Command to delete an entry.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Command to clear all entries.
    /// </summary>
    public ICommand ClearAllCommand { get; }

    /// <summary>
    /// Refreshes the entries from the history service.
    /// </summary>
    public void RefreshEntries()
    {
        var results = string.IsNullOrWhiteSpace(_searchQuery)
            ? _historyService.GetAll()
            : _historyService.Search(_searchQuery);

        _entries = new ObservableCollection<DictationEntry>(results);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Entries)));
    }

    private void OnCopyEntry(DictationEntry? entry)
    {
        if (entry == null) return;

        _clipboardService.SetText(entry.Text ?? string.Empty);
        EntryCopied?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeleteEntry(DictationEntry? entry)
    {
        if (entry == null) return;
        _historyService.Delete(entry.Id);
        RefreshEntries();
    }

    private void OnClearAll()
    {
        _historyService.ClearAll();
        RefreshEntries();
    }

    public void Dispose()
    {
        // No unmanaged resources to clean up
    }
}

/// <summary>
/// Generic relay command implementation for MVVM.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// Generic relay command implementation for MVVM with typed parameter.
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
