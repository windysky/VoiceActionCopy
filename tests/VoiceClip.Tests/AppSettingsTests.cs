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
        settings.SilenceTimeoutSeconds.Should().Be(5);
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
        settings.SilenceTimeoutSeconds = 30;

        // Assert
        settings.SilenceTimeoutSeconds.Should().Be(30);
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

    [Fact]
    public void MaxHistoryEntries_IsClampedToConfiguredRange()
    {
        var settings = new AppSettings
        {
            MaxHistoryEntries = 10
        };

        settings.MaxHistoryEntries.Should().Be(50);

        settings.MaxHistoryEntries = 6000;
        settings.MaxHistoryEntries.Should().Be(5000);
    }

    [Fact]
    public void Clone_AndCopyFrom_RoundTripSettings()
    {
        var settings = new AppSettings
        {
            Language = "de-DE",
            SilenceTimeoutSeconds = 30,
            MaxHistoryEntries = 750,
            RunOnStartup = true
        };

        var clone = settings.Clone();
        var copy = new AppSettings();
        copy.CopyFrom(clone);

        copy.Language.Should().Be("de-DE");
        copy.SilenceTimeoutSeconds.Should().Be(30);
        copy.MaxHistoryEntries.Should().Be(750);
        copy.RunOnStartup.Should().BeTrue();
    }
}
