using FluentAssertions;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class StartupServiceTests
{
    [Fact]
    public void SetStartup_EnableWithoutPath_ReturnsFalse()
    {
        // Arrange
        var service = new StartupService();

        // Act
        var result = service.SetStartup(true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetStartup_Disable_ReturnsCleanly()
    {
        // Arrange
        var service = new StartupService();

        // Act
        var result = service.SetStartup(false);

        // Assert - should not throw, returns true if key existed and was deleted,
        // or true if key didn't exist (no-op)
        result.Should().BeTrue();
    }

    [Fact]
    public void SetStartup_EnableWithEmptyPath_ReturnsFalse()
    {
        // Arrange
        var service = new StartupService();

        // Act
        var result = service.SetStartup(true, "");

        // Assert
        result.Should().BeFalse();
    }
}
