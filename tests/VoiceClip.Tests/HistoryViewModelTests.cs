using FluentAssertions;
using Moq;
using VoiceClip.Models;
using VoiceClip.Services;
using VoiceClip.ViewModels;
using Xunit;

namespace VoiceClip.Tests;

public class HistoryViewModelTests
{
    private readonly Mock<IHistoryService> _historyServiceMock;
    private readonly Mock<IClipboardService> _clipboardServiceMock;
    private readonly HistoryViewModel _viewModel;

    public HistoryViewModelTests()
    {
        _historyServiceMock = new Mock<IHistoryService>();
        _clipboardServiceMock = new Mock<IClipboardService>();
        _historyServiceMock.Setup(s => s.GetAll()).Returns(new List<DictationEntry>().AsReadOnly());
        _historyServiceMock.Setup(s => s.Search(It.IsAny<string>())).Returns(new List<DictationEntry>().AsReadOnly());
        _viewModel = new HistoryViewModel(_historyServiceMock.Object, _clipboardServiceMock.Object);
    }

    [Fact]
    public void Constructor_LoadsEntriesFromService()
    {
        // Arrange
        var entries = new List<DictationEntry>
        {
            new() { Text = "Test entry" }
        }.AsReadOnly();
        _historyServiceMock.Setup(s => s.GetAll()).Returns(entries);

        // Act
        var vm = new HistoryViewModel(_historyServiceMock.Object, _clipboardServiceMock.Object);

        // Assert
        vm.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void SearchQuery_WhenSet_RefreshesEntries()
    {
        // Arrange
        var searchResults = new List<DictationEntry>
        {
            new() { Text = "Found" }
        }.AsReadOnly();
        _historyServiceMock.Setup(s => s.Search("found")).Returns(searchResults);

        // Act
        _viewModel.SearchQuery = "found";

        // Assert
        _viewModel.SearchQuery.Should().Be("found");
        _viewModel.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void CopyCommand_CallsClipboardService()
    {
        // Arrange
        var entry = new DictationEntry { Text = "Copy me" };

        // Act
        _viewModel.CopyCommand.Execute(entry);

        // Assert
        _clipboardServiceMock.Verify(s => s.SetText("Copy me"), Times.Once);
    }

    [Fact]
    public void CopyCommand_RaisesEntryCopiedEvent()
    {
        // Arrange
        var entry = new DictationEntry { Text = "Copy me" };
        var eventRaised = false;
        _viewModel.EntryCopied += (s, e) => eventRaised = true;

        // Act
        _viewModel.CopyCommand.Execute(entry);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void DeleteCommand_CallsHistoryServiceDelete()
    {
        // Arrange
        var entry = new DictationEntry { Id = Guid.NewGuid(), Text = "Delete me" };

        // Act
        _viewModel.DeleteCommand.Execute(entry);

        // Assert
        _historyServiceMock.Verify(s => s.Delete(entry.Id), Times.Once);
    }

    [Fact]
    public void ClearAllCommand_CallsHistoryServiceClearAll()
    {
        // Act
        _viewModel.ClearAllCommand.Execute(null);

        // Assert
        _historyServiceMock.Verify(s => s.ClearAll(), Times.Once);
    }

    [Fact]
    public void SelectedEntry_SetRaisesPropertyChanged()
    {
        // Arrange
        var entry = new DictationEntry { Text = "Selected" };
        string? changedProperty = null;
        _viewModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        // Act
        _viewModel.SelectedEntry = entry;

        // Assert
        _viewModel.SelectedEntry.Should().Be(entry);
        changedProperty.Should().Be(nameof(_viewModel.SelectedEntry));
    }

    [Fact]
    public void RefreshEntries_WithEmptyQuery_LoadsAll()
    {
        // Arrange
        var allEntries = new List<DictationEntry>
        {
            new() { Text = "Entry 1" },
            new() { Text = "Entry 2" }
        }.AsReadOnly();
        _historyServiceMock.Setup(s => s.GetAll()).Returns(allEntries);

        // Act
        _viewModel.RefreshEntries();

        // Assert
        _viewModel.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void CopyCommand_WithNullEntry_DoesNotThrow()
    {
        // Act
        Action act = () => _viewModel.CopyCommand.Execute(null);

        // Assert
        act.Should().NotThrow();
        _clipboardServiceMock.Verify(s => s.SetText(It.IsAny<string>()), Times.Never);
    }
}
