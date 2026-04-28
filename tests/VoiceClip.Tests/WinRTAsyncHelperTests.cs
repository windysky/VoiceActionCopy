using FluentAssertions;
using VoiceClip.Helpers;
using Xunit;

namespace VoiceClip.Tests;

public class WinRTAsyncHelperTests
{
    [Fact]
    public void AsTask_WithNullAction_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => WinRTAsyncHelper.AsTask(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AsTask_WithTask_ReturnsWrappedTask()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        object action = tcs.Task;

        // Act
        var result = WinRTAsyncHelper.AsTask(action);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void TryAsTask_WithNullAction_ReturnsFalse()
    {
        // Act
        var result = WinRTAsyncHelper.TryAsTask(null, out var task);

        // Assert
        result.Should().BeFalse();
        task.Should().BeNull();
    }

    [Fact]
    public void TryAsTask_WithTask_ReturnsTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        object action = tcs.Task;

        // Act
        var result = WinRTAsyncHelper.TryAsTask(action, out var task);

        // Assert
        result.Should().BeTrue();
        task.Should().NotBeNull();
    }
}
