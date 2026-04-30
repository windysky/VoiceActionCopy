using FluentAssertions;
using VoiceClip.Helpers;
using Xunit;

namespace VoiceClip.Tests;

public class AudioDeviceHelperTests
{
    [Fact]
    public void AudioDevice_Record_StoresIdAndName()
    {
        var device = new AudioDevice("test-id", "Test Mic");
        device.Id.Should().Be("test-id");
        device.Name.Should().Be("Test Mic");
    }

    [Fact]
    public async Task GetInputDevicesAsync_ReturnsNonNullList()
    {
        var devices = await AudioDeviceHelper.GetInputDevicesAsync();
        devices.Should().NotBeNull();
    }
}
