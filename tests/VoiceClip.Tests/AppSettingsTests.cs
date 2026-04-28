using FluentAssertions;
using VoiceClip.Models;
using Xunit;

namespace VoiceClip.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultSettings_HaveCorrectDefaults()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.Language.Should().Be("en-US");
        settings.SilenceTimeoutSeconds.Should().Be(60);
        settings.MaxHistoryEntries.Should().Be(500);
        settings.RunOnStartup.Should().BeFalse();
        settings.DictateHotkey.Should().Be("Ctrl+Alt+D");
        settings.HistoryHotkey.Should().Be("Ctrl+Alt+V");
    }

    [Fact]
    public void SilenceTimeout_CanBeSetWithinRange()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.SilenceTimeoutSeconds = 120;

        // Assert
        settings.SilenceTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void MaxHistoryEntries_CanBeSetWithinRange()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.MaxHistoryEntries = 1000;

        // Assert
        settings.MaxHistoryEntries.Should().Be(1000);
    }

    [Fact]
    public void Language_CanBeChanged()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.Language = "ko-KR";

        // Assert
        settings.Language.Should().Be("ko-KR");
    }
}
