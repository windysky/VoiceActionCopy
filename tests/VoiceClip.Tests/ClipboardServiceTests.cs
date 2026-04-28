using FluentAssertions;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class ClipboardServiceTests
{
    [Fact]
    public void SetText_WithNullText_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new ClipboardService();

        // Act
        Action act = () => service.SetText(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Implements_IClipboardService()
    {
        // Arrange & Act
        IClipboardService service = new ClipboardService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IClipboardService>();
    }
}
