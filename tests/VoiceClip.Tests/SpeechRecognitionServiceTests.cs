using FluentAssertions;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class SpeechRecognitionServiceTests
{
    [Fact]
    public void Implements_ISpeechRecognitionService()
    {
        // Arrange & Act
        ISpeechRecognitionService service = new SpeechRecognitionService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ISpeechRecognitionService>();
    }

    [Fact]
    public void IsRecording_InitiallyFalse()
    {
        // Arrange
        var service = new SpeechRecognitionService();

        // Assert
        service.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void AppendRecognizedText_RaisesPartialResultReceived()
    {
        // Arrange
        var service = new SpeechRecognitionService();
        PartialResultEventArgs? receivedArgs = null;
        service.PartialResultReceived += (s, e) => receivedArgs = e;

        // Act
        service.AppendRecognizedText("Hello");

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Text.Should().Be("Hello");
    }

    [Fact]
    public void AppendRecognizedText_WithNullOrEmpty_DoesNotRaiseEvent()
    {
        // Arrange
        var service = new SpeechRecognitionService();
        var eventCount = 0;
        service.PartialResultReceived += (s, e) => eventCount++;

        // Act
        service.AppendRecognizedText("");
        service.AppendRecognizedText(null!);

        // Assert
        eventCount.Should().Be(0);
    }

    [Fact]
    public async Task StopDictation_WhenNotRecording_ReturnsEmptyString()
    {
        // Arrange
        var service = new SpeechRecognitionService();

        // Act
        var result = await service.StopDictationAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task StopDictation_WhenNotRecording_DoesNotRaiseEvent()
    {
        // Arrange
        var service = new SpeechRecognitionService();
        DictationResultEventArgs? receivedArgs = null;
        service.DictationCompleted += (s, e) => receivedArgs = e;

        // Act
        await service.StopDictationAsync();

        // Assert
        receivedArgs.Should().BeNull();
    }
}
