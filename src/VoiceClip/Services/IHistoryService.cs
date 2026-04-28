using VoiceClip.Models;

namespace VoiceClip.Services;

/// <summary>
/// Interface for history persistence operations.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Gets all dictation entries in reverse chronological order.
    /// </summary>
    IReadOnlyList<DictationEntry> GetAll();

    /// <summary>
    /// Adds a new dictation entry to the history.
    /// Automatically trims to MaxHistoryEntries if exceeded.
    /// </summary>
    DictationEntry Add(string text, double durationSeconds);

    /// <summary>
    /// Deletes a specific entry by ID.
    /// </summary>
    bool Delete(Guid id);

    /// <summary>
    /// Deletes all entries.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Searches entries by text content.
    /// </summary>
    IReadOnlyList<DictationEntry> Search(string query);
}
