using System.IO;
using FluentAssertions;
using VoiceClip.Models;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class HistoryServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"VoiceClip_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _service = new HistoryService(_testDir, maxEntries: 10);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public void GetAll_WhenNoFile_ReturnsEmptyList()
    {
        // Act
        var entries = _service.GetAll();

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void Add_CreatesEntryAndPersists()
    {
        // Act
        var entry = _service.Add("Hello world", 3.5);

        // Assert
        entry.Text.Should().Be("Hello world");
        entry.DurationSeconds.Should().Be(3.5);
        entry.Id.Should().NotBe(Guid.Empty);
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persisted
        var all = _service.GetAll();
        all.Should().HaveCount(1);
        all[0].Text.Should().Be("Hello world");
    }

    [Fact]
    public void Add_ReturnsEntriesInReverseChronologicalOrder()
    {
        // Act
        _service.Add("First", 1.0);
        _service.Add("Second", 2.0);
        _service.Add("Third", 3.0);

        // Assert
        var all = _service.GetAll();
        all.Should().HaveCount(3);
        all[0].Text.Should().Be("Third");
        all[1].Text.Should().Be("Second");
        all[2].Text.Should().Be("First");
    }

    [Fact]
    public void Add_TrimToMaxEntries_RemovesOldest()
    {
        // Arrange - max is 10 entries
        for (int i = 0; i < 12; i++)
        {
            _service.Add($"Entry {i}", 1.0);
        }

        // Act
        var all = _service.GetAll();

        // Assert
        all.Should().HaveCount(10);
        all[0].Text.Should().Be("Entry 11");  // newest first
        all[9].Text.Should().Be("Entry 2");   // oldest kept
    }

    [Fact]
    public void Delete_ExistingId_ReturnsTrueAndRemoves()
    {
        // Arrange
        var entry = _service.Add("To delete", 1.0);

        // Act
        var result = _service.Delete(entry.Id);

        // Assert
        result.Should().BeTrue();
        _service.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Delete_NonExistingId_ReturnsFalse()
    {
        // Act
        var result = _service.Delete(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ClearAll_RemovesAllEntries()
    {
        // Arrange
        _service.Add("First", 1.0);
        _service.Add("Second", 2.0);

        // Act
        _service.ClearAll();

        // Assert
        _service.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Search_FindsMatchingEntries()
    {
        // Arrange
        _service.Add("Hello world", 1.0);
        _service.Add("Goodbye moon", 2.0);
        _service.Add("Hello universe", 3.0);

        // Act
        var results = _service.Search("Hello");

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(e => e.Text == "Hello world");
        results.Should().Contain(e => e.Text == "Hello universe");
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        // Arrange
        _service.Add("Hello World", 1.0);

        // Act
        var results = _service.Search("hello");

        // Assert
        results.Should().HaveCount(1);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAll()
    {
        // Arrange
        _service.Add("First", 1.0);
        _service.Add("Second", 2.0);

        // Act
        var results = _service.Search("");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public void Persistence_SurvivesNewServiceInstance()
    {
        // Arrange
        _service.Add("Persistent text", 5.0);

        // Act - create new service pointing to same directory
        var newService = new HistoryService(_testDir, maxEntries: 10);
        var entries = newService.GetAll();

        // Assert
        entries.Should().HaveCount(1);
        entries[0].Text.Should().Be("Persistent text");
    }
}
