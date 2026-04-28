using FluentAssertions;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class HotkeyServiceTests
{
    [Fact]
    public void Implements_IHotkeyService()
    {
        // Arrange & Act
        IHotkeyService service = new HotkeyService(nint.Zero);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IHotkeyService>();
    }

    [Fact]
    public void ProcessHotkeyMessage_RaisesHotkeyPressedEvent()
    {
        // Arrange
        var service = new HotkeyService(nint.Zero);
        HotkeyEventArgs? receivedArgs = null;
        service.HotkeyPressed += (s, e) => receivedArgs = e;

        // Act
        service.ProcessHotkeyMessage(HotkeyService.HOTKEY_DICTATE);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.Id.Should().Be(HotkeyService.HOTKEY_DICTATE);
    }

    [Fact]
    public void HotkeyEventArgs_IdIsSet()
    {
        // Arrange
        var args = new HotkeyEventArgs { Id = 42 };

        // Assert
        args.Id.Should().Be(42);
    }

    [Fact]
    public void Dispose_CleansUpWithoutError()
    {
        // Arrange
        var service = new HotkeyService(nint.Zero);

        // Act
        Action act = () => service.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ModifierConstants_HaveCorrectValues()
    {
        HotkeyService.MOD_ALT.Should().Be(0x0001);
        HotkeyService.MOD_CONTROL.Should().Be(0x0002);
        HotkeyService.MOD_SHIFT.Should().Be(0x0004);
        HotkeyService.MOD_WIN.Should().Be(0x0008);
    }

    [Fact]
    public void VirtualKeyConstants_HaveCorrectValues()
    {
        HotkeyService.VK_D.Should().Be(0x44);
        HotkeyService.VK_V.Should().Be(0x56);
    }
}
