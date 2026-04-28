using System.IO;
using FluentAssertions;
using VoiceClip.Models;
using VoiceClip.Services;
using Xunit;

namespace VoiceClip.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"VoiceClip_Settings_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _service = new SettingsService(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Fact]
    public void Load_WhenNoFile_ReturnsDefaults()
    {
        // Act
        var settings = _service.Load();

        // Assert
        settings.Should().NotBeNull();
        settings.Language.Should().Be("en-US");
        settings.SilenceTimeoutSeconds.Should().Be(60);
        settings.MaxHistoryEntries.Should().Be(500);
    }

    [Fact]
    public void Save_AndLoad_RoundTripsCorrectly()
    {
        // Arrange
        var settings = new AppSettings
        {
            Language = "ko-KR",
            SilenceTimeoutSeconds = 120,
            MaxHistoryEntries = 1000,
            RunOnStartup = true
        };

        // Act
        _service.Save(settings);
        var loaded = _service.Load();

        // Assert
        loaded.Language.Should().Be("ko-KR");
        loaded.SilenceTimeoutSeconds.Should().Be(120);
        loaded.MaxHistoryEntries.Should().Be(1000);
        loaded.RunOnStartup.Should().BeTrue();
    }

    [Fact]
    public void Save_WithNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _service.Save(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Implements_ISettingsService()
    {
        ISettingsService service = new SettingsService(_testDir);
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ISettingsService>();
    }
}
