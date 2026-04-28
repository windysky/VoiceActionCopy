using System.IO;
using System.Text.Json;
using VoiceClip.Models;

namespace VoiceClip.Services;

/// <summary>
/// JSON file-based persistence for dictation history.
/// Stores entries at {storageDir}/history.json.
/// </summary>
public class HistoryService : IHistoryService
{
    private readonly string _filePath;
    private readonly int _maxEntries;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<DictationEntry> _entries;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new HistoryService.
    /// </summary>
    /// <param name="storageDir">Directory path for the history file.</param>
    /// <param name="maxEntries">Maximum number of entries to retain.</param>
    public HistoryService(string storageDir, int maxEntries = 500)
    {
        _filePath = Path.Combine(storageDir, "history.json");
        _maxEntries = maxEntries;
        _entries = LoadFromFile();
    }

    /// <inheritdoc/>
    public IReadOnlyList<DictationEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.OrderByDescending(e => e.Timestamp).ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    public DictationEntry Add(string text, double durationSeconds)
    {
        var entry = new DictationEntry
        {
            Id = Guid.NewGuid(),
            Text = text,
            Timestamp = DateTime.UtcNow,
            DurationSeconds = durationSeconds
        };

        lock (_lock)
        {
            _entries.Add(entry);
            TrimToMaxEntries();
            SaveToFile();
        }

        return entry;
    }

    /// <inheritdoc/>
    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            var removed = _entries.RemoveAll(e => e.Id == id);
            if (removed > 0)
            {
                SaveToFile();
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc/>
    public void ClearAll()
    {
        lock (_lock)
        {
            _entries.Clear();
            SaveToFile();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DictationEntry> Search(string query)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return _entries.OrderByDescending(e => e.Timestamp).ToList().AsReadOnly();
            }

            return _entries
                .Where(e => e.Text?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(e => e.Timestamp)
                .ToList()
                .AsReadOnly();
        }
    }

    private void TrimToMaxEntries()
    {
        if (_entries.Count > _maxEntries)
        {
            _entries = _entries
                .OrderByDescending(e => e.Timestamp)
                .Take(_maxEntries)
                .ToList();
        }
    }

    private List<DictationEntry> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<DictationEntry>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveToFile()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_entries, _jsonOptions);
            var tempFile = _filePath + ".tmp";
            File.WriteAllText(tempFile, json);
            if (File.Exists(_filePath))
            {
                File.Replace(tempFile, _filePath, null);
            }
            else
            {
                File.Move(tempFile, _filePath);
            }
        }
        catch
        {
            // Silently fail - history is best-effort persistence
        }
    }
}
