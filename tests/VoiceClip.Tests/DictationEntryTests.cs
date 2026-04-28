using FluentAssertions;
using VoiceClip.Models;
using Xunit;

namespace VoiceClip.Tests;

public class DictationEntryTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var text = "Hello world";
        var timestamp = DateTime.UtcNow;
        var durationSeconds = 5.2;

        // Act
        var entry = new DictationEntry
        {
            Id = id,
            Text = text,
            Timestamp = timestamp,
            DurationSeconds = durationSeconds
        };

        // Assert
        entry.Id.Should().Be(id);
        entry.Text.Should().Be(text);
        entry.Timestamp.Should().Be(timestamp);
        entry.DurationSeconds.Should().Be(durationSeconds);
    }

    [Fact]
    public void Text_TruncatesForPreview()
    {
        // Arrange
        var longText = new string('a', 200);
        var entry = new DictationEntry
        {
            Id = Guid.NewGuid(),
            Text = longText,
            Timestamp = DateTime.UtcNow,
            DurationSeconds = 10.0
        };

        // Act
        var preview = entry.Preview;

        // Assert
        preview.Should().HaveLength(80);
        preview.Should().Be(new string('a', 77) + "...");
    }

    [Fact]
    public void Preview_ShortText_ReturnsFullText()
    {
        // Arrange
        var shortText = "Hello";
        var entry = new DictationEntry
        {
            Id = Guid.NewGuid(),
            Text = shortText,
            Timestamp = DateTime.UtcNow,
            DurationSeconds = 2.0
        };

        // Act
        var preview = entry.Preview;

        // Assert
        preview.Should().Be(shortText);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var entry = new DictationEntry();

        // Assert
        entry.Id.Should().Be(Guid.Empty);
        entry.Text.Should().BeNull();
        entry.DurationSeconds.Should().Be(0);
    }
}
